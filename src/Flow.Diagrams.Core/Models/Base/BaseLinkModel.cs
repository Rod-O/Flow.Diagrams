using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using System;
using System.Collections.Generic;
using Flow.Diagrams.Core.PathGenerators;
using Flow.Diagrams.Core.Routers;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>
/// A connection between two <see cref="Anchor"/>s. It owns the route, path, vertices, labels and markers; the
/// concrete <see cref="LinkModel"/> adds styling. Links can attach to other links, which are then listed in <see
/// cref="Links"/>.
/// </summary>
public abstract class BaseLinkModel : SelectableModel, IHasBounds, ILinkable
{
    private readonly List<BaseLinkModel> _links = new();

    /// <summary>Raised by <see cref="SetSource"/> with the link, the old anchor and the new anchor.</summary>
    public event Action<BaseLinkModel, Anchor, Anchor>? SourceChanged;
    /// <summary>Raised by <see cref="SetTarget"/> with the link, the old anchor and the new anchor.</summary>
    public event Action<BaseLinkModel, Anchor, Anchor>? TargetChanged;
    /// <summary>Raised when a link the user dragged is dropped on a target and attached to it.</summary>
    public event Action<BaseLinkModel>? TargetAttached;

    /// <summary>Creates a link between the two anchors with a generated <see cref="Model.Id"/>.</summary>
    protected BaseLinkModel(Anchor source, Anchor target)
    {
        Source = source;
        Target = target;
    }

    /// <summary>Creates a link with the given <paramref name="id"/> between the two anchors.</summary>
    protected BaseLinkModel(string id, Anchor source, Anchor target) : base(id)
    {
        Source = source;
        Target = target;
    }

    /// <summary>The anchor at the start of the link; change it with <see cref="SetSource"/>.</summary>
    public Anchor Source { get; private set; }
    /// <summary>The anchor at the end of the link; change it with <see cref="SetTarget"/>.</summary>
    public Anchor Target { get; private set; }
    /// <summary>
    /// The diagram that contains the link, or <c>null</c> while it is not in <see cref="Diagram.Links"/>. A link
    /// outside a diagram has no route or path.
    /// </summary>
    public Diagram? Diagram { get; internal set; }
    /// <summary>
    /// The points the router produced at the last <see cref="Refresh"/>, or <c>null</c> when the link has no diagram
    /// or an anchor has no position.
    /// </summary>
    public Point[]? Route { get; private set; }
    /// <summary>
    /// The drawable path generated from <see cref="Route"/> at the last <see cref="Refresh"/>, or <c>null</c> when
    /// there is no route.
    /// </summary>
    public PathGeneratorResult? PathGeneratorResult { get; private set; }
    /// <summary>
    /// Whether both ends are attached to models; <c>false</c> while an end is a free <see cref="PositionAnchor"/>,
    /// such as during a drag.
    /// </summary>
    public bool IsAttached => Source is not PositionAnchor && Target is not PositionAnchor;
    /// <summary>
    /// The router of this link; <c>null</c> uses <see cref="Options.DiagramLinkOptions.DefaultRouter"/>. Takes effect
    /// on the next <see cref="Refresh"/>.
    /// </summary>
    public Router? Router { get; set; }
    /// <summary>
    /// The path generator of this link; <c>null</c> uses <see
    /// cref="Options.DiagramLinkOptions.DefaultPathGenerator"/>. Takes effect on the next <see cref="Refresh"/>.
    /// </summary>
    public PathGenerator? PathGenerator { get; set; }
    /// <summary>The marker drawn at the source end, or <c>null</c> for none.</summary>
    public LinkMarker? SourceMarker { get; set; }
    /// <summary>The marker drawn at the target end, or <c>null</c> for none.</summary>
    public LinkMarker? TargetMarker { get; set; }
    /// <summary>
    /// Whether pressing the pointer on a segment of the link creates a vertex there and starts dragging it.
    /// </summary>
    public bool Segmentable { get; set; } = false;
    /// <summary>
    /// The waypoints the route passes through, in order from source to target. Prefer <see cref="AddVertex"/>;
    /// editing the list does not refresh the link.
    /// </summary>
    public List<LinkVertexModel> Vertices { get; } = new();

    /// <summary>Where this link crosses links drawn below it; set by <see cref="Behaviors.LinkHalosBehavior"/>.</summary>
    public IReadOnlyList<LinkHalo> Halos { get; private set; } = Array.Empty<LinkHalo>();
    /// <summary>The labels drawn along the link. Prefer <see cref="AddLabel"/>.</summary>
    public List<LinkLabelModel> Labels { get; } = new();
    /// <summary>The links attached to this link.</summary>
    public IReadOnlyList<BaseLinkModel> Links => _links;

    /// <summary>
    /// Recomputes <see cref="Route"/> and <see cref="PathGeneratorResult"/>, then raises <see cref="Model.Changed"/>.
    /// </summary>
    public override void Refresh()
    {
        GeneratePath();
        base.Refresh();
    }

    /// <summary>Refreshes each link attached to this link.</summary>
    public void RefreshLinks()
    {
        foreach (var link in Links)
        {
            link.Refresh();
        }
    }

    /// <summary>
    /// Creates a label with the given text and position, appends it to <see cref="Labels"/> and returns it. Does not
    /// refresh the link. See <see cref="LinkLabelModel.Distance"/> for <paramref name="distance"/>.
    /// </summary>
    public LinkLabelModel AddLabel(string content, double? distance = null, Point? offset = null)
    {
        var label = new LinkLabelModel(this, content, distance, offset);
        Labels.Add(label);
        return label;
    }

    /// <summary>
    /// Creates a vertex at <paramref name="position"/> (the origin when <c>null</c>), appends it to <see
    /// cref="Vertices"/> and returns it. Does not refresh the link.
    /// </summary>
    public LinkVertexModel AddVertex(Point? position = null)
    {
        var vertex = new LinkVertexModel(this, position);
        Vertices.Add(vertex);
        return vertex;
    }

    /// <summary>
    /// Re-attaches the start of the link to <paramref name="anchor"/> and raises <see cref="SourceChanged"/>; does
    /// nothing if it is the current anchor. Throws <see cref="ArgumentNullException"/> for <c>null</c>. Does not
    /// refresh the link.
    /// </summary>
    public void SetSource(Anchor anchor)
    {
        ArgumentNullException.ThrowIfNull(anchor, nameof(anchor));

        if (Source == anchor)
            return;

        var old = Source;
        Source = anchor;
        SourceChanged?.Invoke(this, old, Source);
    }

    /// <summary>
    /// Re-attaches the end of the link to <paramref name="anchor"/> and raises <see cref="TargetChanged"/>; does
    /// nothing if it is the current anchor. Does not refresh the link.
    /// </summary>
    public void SetTarget(Anchor anchor)
    {
        if (Target == anchor)
            return;

        var old = Target;
        Target = anchor;
        TargetChanged?.Invoke(this, old, Target);
    }

    /// <summary>
    /// The bounding box of the generated path in diagram units, or <c>null</c> before a path exists.
    /// </summary>
    public Rectangle? GetBounds()
    {
        if (PathGeneratorResult == null)
            return null;

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        var path = PathGeneratorResult.FullPath;
        var bbox = path.GetBBox();
        minX = Math.Min(minX, bbox.Left);
        minY = Math.Min(minY, bbox.Top);
        maxX = Math.Max(maxX, bbox.Right);
        maxY = Math.Max(maxY, bbox.Bottom);

        return new Rectangle(minX, minY, maxX, maxY);
    }

    /// <summary>Always <c>true</c>: a link that starts on another link may end on anything.</summary>
    public bool CanAttachTo(ILinkable other) => true;

    /// <summary>Replaces the halos and redraws the link, without routing it again.</summary>
    public void SetHalos(IReadOnlyList<LinkHalo> halos)
    {
        if (Halos.SequenceEqual(halos))
            return;

        Halos = halos;
        base.Refresh();
    }

    /// <summary>
    /// Triggers the TargetAttached event
    /// </summary>
    public void TriggerTargetAttached() => TargetAttached?.Invoke(this);

    private void GeneratePath()
    {
        if (Diagram != null)
        {
            var router = Router ?? Diagram.Options.Links.DefaultRouter;
            var pathGenerator = PathGenerator ?? Diagram.Options.Links.DefaultPathGenerator;
            var route = router.GetRoute(Diagram, this);
            var source = Source.GetPosition(this, route);
            var target = Target.GetPosition(this, route);
            if (source != null && target != null)
            {
                Route = route;
                PathGeneratorResult = pathGenerator.GetResult(Diagram, this, route, source, target);
                return;
            }
        }

        Route = null;
        PathGeneratorResult = null;
    }

    void ILinkable.AddLink(BaseLinkModel link) => _links.Add(link);

    void ILinkable.RemoveLink(BaseLinkModel link) => _links.Remove(link);
}
