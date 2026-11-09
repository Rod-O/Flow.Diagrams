using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Events;
using System.Linq;
using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Creates links by dragging: pressing the left button on a port that is not locked, on a node that is not locked, starts a link from it, which follows the pointer and
/// snaps to a nearby port when <see cref="Options.DiagramLinkOptions.EnableSnapping"/> is on. Releasing over a linkable model attaches the
/// target; releasing elsewhere removes the link when <see cref="Options.DiagramLinkOptions.RequireTarget"/> is set. Registered by default;
/// controls such as <see cref="Controls.Default.DragNewLinkControl"/> start drags through <see cref="StartFrom(ILinkable, double, double)"/>.
/// </summary>
public class DragNewLinkBehavior : Behavior
{
    private PositionAnchor? _targetPositionAnchor;

    /// <summary>The link being dragged; null when no drag is in progress.</summary>
    public BaseLinkModel? OngoingLink { get; private set; }

    /// <summary>Starts listening to pointer events of <paramref name="diagram"/>.</summary>
    public DragNewLinkBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.PointerDown += OnPointerDown;
        Diagram.PointerMove += OnPointerMove;
        Diagram.PointerUp += OnPointerUp;
    }

    /// <summary>
    /// Creates a link from <paramref name="source"/> with <see cref="Options.DiagramLinkOptions.Factory"/> and begins dragging its
    /// target; does nothing when a drag is already in progress.
    /// </summary>
    /// <param name="source">The model the link starts from.</param>
    /// <param name="clientX">Pointer X in screen pixels, as <c>ClientX</c> of <see cref="Events.PointerEventArgs"/>.</param>
    /// <param name="clientY">Pointer Y in screen pixels.</param>
    public void StartFrom(ILinkable source, double clientX, double clientY)
    {
        if (OngoingLink != null)
            return;

        _targetPositionAnchor = new PositionAnchor(CalculateTargetPosition(clientX, clientY));
        OngoingLink = Diagram.Options.Links.Factory(Diagram, source, _targetPositionAnchor);
        if (OngoingLink == null)
            return;

        Diagram.Links.Add(OngoingLink);
    }

    /// <summary>
    /// Detaches the target of an existing <paramref name="link"/> and drags it, so an attached end can be moved to another
    /// model; does nothing when a drag is already in progress.
    /// </summary>
    /// <param name="link">The link whose target is dragged.</param>
    /// <param name="clientX">Pointer X in screen pixels, as <c>ClientX</c> of <see cref="Events.PointerEventArgs"/>.</param>
    /// <param name="clientY">Pointer Y in screen pixels.</param>
    public void StartFrom(BaseLinkModel link, double clientX, double clientY)
    {
        if (OngoingLink != null)
            return;

        _targetPositionAnchor = new PositionAnchor(CalculateTargetPosition(clientX, clientY));
        OngoingLink = link;
        OngoingLink.SetTarget(_targetPositionAnchor);
        OngoingLink.Refresh();
        OngoingLink.RefreshLinks();
    }

    private void OnPointerDown(Model? model, MouseEventArgs e)
    {
        if (e.Button != (int)MouseEventButton.Left)
            return;

        OngoingLink = null;
        _targetPositionAnchor = null;

        if (model is PortModel port)
        {
            if (port.Locked || port.Parent.Locked)
                return;

            _targetPositionAnchor = new PositionAnchor(CalculateTargetPosition(e.ClientX, e.ClientY));
            OngoingLink = Diagram.Options.Links.Factory(Diagram, port, _targetPositionAnchor);
            if (OngoingLink == null)
                return;

            OngoingLink.SetTarget(_targetPositionAnchor);
            Diagram.Links.Add(OngoingLink);
        }
    }

    private void OnPointerMove(Model? model, MouseEventArgs e)
    {
        if (OngoingLink == null || model != null)
            return;

        _targetPositionAnchor!.SetPosition(CalculateTargetPosition(e.ClientX, e.ClientY));

        if (Diagram.Options.Links.EnableSnapping)
        {
            var nearPort = FindNearPortToAttachTo();
            if (nearPort != null || OngoingLink.Target is not PositionAnchor)
            {
                OngoingLink.SetTarget(nearPort is null ? _targetPositionAnchor : new SinglePortAnchor(nearPort));
            }
        }

        OngoingLink.Refresh();
        OngoingLink.RefreshLinks();
    }

    private void OnPointerUp(Model? model, MouseEventArgs e)
    {
        if (OngoingLink == null)
            return;

        if (OngoingLink.IsAttached) // Snapped already
        {
            OngoingLink.TriggerTargetAttached();
            OngoingLink = null;
            return;
        }

        if (model is ILinkable linkable && (OngoingLink.Source.Model == null || OngoingLink.Source.Model.CanAttachTo(linkable)))
        {
            var targetAnchor = Diagram.Options.Links.TargetAnchorFactory(Diagram, OngoingLink, linkable);
            OngoingLink.SetTarget(targetAnchor);
            OngoingLink.TriggerTargetAttached();
            OngoingLink.Refresh();
            OngoingLink.RefreshLinks();
        }
        else if (Diagram.Options.Links.RequireTarget)
        {
            Diagram.Links.Remove(OngoingLink);
        }
        else if (!Diagram.Options.Links.RequireTarget)
        {
            OngoingLink.Refresh();
        }

        OngoingLink = null;
    }

    private Point CalculateTargetPosition(double clientX, double clientY)
    {
        var target = Diagram.GetRelativeMousePoint(clientX, clientY);

        if (OngoingLink == null)
        {
            return target;
        }

        var source = OngoingLink.Source.GetPlainPosition()!;
        var dirVector = target.Subtract(source).Normalize();
        var change = dirVector.Multiply(5);
        return target.Subtract(change);
    }

    private PortModel? FindNearPortToAttachTo()
    {
        if (OngoingLink is null || _targetPositionAnchor is null)
            return null;

        PortModel? nearestSnapPort = null;
        var nearestSnapPortDistance = double.PositiveInfinity;

        var position = _targetPositionAnchor!.GetPosition(OngoingLink)!;

        foreach (var port in Diagram.Nodes.SelectMany((NodeModel n) => n.Ports))
        {
            var distance = position.DistanceTo(port.Position);

            if (distance <= Diagram.Options.Links.SnappingRadius && (OngoingLink.Source.Model?.CanAttachTo(port) != false))
            {
                if (distance < nearestSnapPortDistance)
                {
                    nearestSnapPortDistance = distance;
                    nearestSnapPort = port;
                }
            }
        }

        return nearestSnapPort;
    }

    /// <summary>Stops listening to pointer events.</summary>
    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;
        Diagram.PointerMove -= OnPointerMove;
        Diagram.PointerUp -= OnPointerUp;
    }
}