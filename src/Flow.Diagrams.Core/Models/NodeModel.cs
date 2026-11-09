using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Diagrams.Core.Models;

/// <summary>
/// A box in the diagram that can hold ports and be linked. Its size is measured by the renderer from the rendered
/// content unless <see cref="ControlledSize"/> is set. Subclass it, and register a widget for the subclass, to draw
/// custom nodes.
/// </summary>
public class NodeModel : MovableModel, IHasBounds, IHasShape, ILinkable
{
    private readonly List<PortModel> _ports = new();
    private readonly List<BaseLinkModel> _links = new();
    private Size? _size;

    /// <summary>
    /// Raised when <see cref="Size"/> is set to a different value, normally after the renderer measures the node.
    /// </summary>
    public event Action<NodeModel>? SizeChanged;
    /// <summary>
    /// Raised by <see cref="SetPosition"/> on every move (the argument is the node); a containing <see
    /// cref="GroupModel"/> listens to resize itself. Not raised by <see cref="UpdatePositionSilently"/>.
    /// </summary>
    public event Action<NodeModel>? Moving;

    /// <summary>Creates a node at <paramref name="position"/>, or at the origin when <c>null</c>.</summary>
    public NodeModel(Point? position = null) : base(position)
    {
    }

    /// <summary>
    /// Creates a node with the given <paramref name="id"/> at <paramref name="position"/>, or at the origin when
    /// <c>null</c>.
    /// </summary>
    public NodeModel(string id, Point? position = null) : base(id, position)
    {
    }

    /// <summary>
    /// The node's size in diagram units, or <c>null</c> until it is measured or assigned; bounds, ports and links
    /// need it. Raises <see cref="SizeChanged"/> when it changes; it does not refresh the node, so to resize a <see
    /// cref="ControlledSize"/> node use <see cref="SetSize"/>.
    /// </summary>
    public Size? Size
    {
        get => _size;
        set
        {
            if (value?.Equals(_size) == true)
                return;

            _size = value;
            SizeChanged?.Invoke(this);
        }
    }
    /// <summary>
    /// Whether you set <see cref="Size"/> yourself; when <c>true</c> the renderer does not measure the node. Can only
    /// be set in an object initializer.
    /// </summary>
    public bool ControlledSize { get; init; }

    /// <summary>
    /// Sets <see cref="Size"/>, then refreshes the node and the links attached to it and calls <see
    /// cref="ReinitializePorts"/> so the ports are measured at their new places: what the renderer does when it measures
    /// a new size, for a <see cref="ControlledSize"/> node. Does nothing when <paramref name="size"/> equals the current size.
    /// </summary>
    public void SetSize(Size size)
    {
        if (size.Equals(_size))
            return;

        Size = size;
        Refresh();
        RefreshLinks();
        ReinitializePorts();
    }

    /// <summary>The group that contains this node, or <c>null</c>.</summary>
    public GroupModel? Group { get; internal set; }
    /// <summary>
    /// The text the default node widget shows ("Title" when <c>null</c>); custom widgets may ignore it. Setting it does
    /// not redraw the node: call <see cref="Model.Refresh"/>.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>The ports of this node, in the order they were added.</summary>
    public IReadOnlyList<PortModel> Ports => _ports;
    /// <summary>
    /// The links attached to the node itself; links attached to its ports are in <see cref="PortLinks"/>.
    /// </summary>
    public IReadOnlyList<BaseLinkModel> Links => _links;
    /// <summary>The links attached to any of this node's ports.</summary>
    public IEnumerable<BaseLinkModel> PortLinks => Ports.SelectMany(p => p.Links);

    #region Ports

    /// <summary>
    /// Adds <paramref name="port"/> and returns it. The port should have been created with this node as its <see
    /// cref="PortModel.Parent"/>.
    /// </summary>
    public PortModel AddPort(PortModel port)
    {
        _ports.Add(port);
        return port;
    }

    /// <summary>Creates a port with the given alignment, adds it and returns it.</summary>
    public PortModel AddPort(PortAlignment alignment = PortAlignment.Bottom)
        => AddPort(new PortModel(this, alignment, Position));

    /// <summary>The first port with <paramref name="alignment"/>, or <c>null</c> if there is none.</summary>
    public PortModel? GetPort(PortAlignment alignment) => Ports.FirstOrDefault(p => p.Alignment == alignment);

    /// <summary>
    /// Like <see cref="GetPort(PortAlignment)"/>, cast to <typeparamref name="T"/>; throws <see
    /// cref="InvalidCastException"/> if the port is of another type.
    /// </summary>
    public T? GetPort<T>(PortAlignment alignment) where T : PortModel => (T?)GetPort(alignment);

    /// <summary>Removes the port and returns whether it was present. Links attached to it are not removed.</summary>
    public bool RemovePort(PortModel port) => _ports.Remove(port);

    #endregion

    #region Refreshing

    /// <summary>Refreshes the node and each of its ports with their links.</summary>
    public void RefreshAll()
    {
        Refresh();
        _ports.ForEach(p => p.RefreshAll());
    }

    /// <summary>Refreshes the links attached to the node and the links attached to those.</summary>
    public void RefreshLinks()
    {
        foreach (var link in Links)
        {
            link.Refresh();
            link.RefreshLinks();
        }
    }

    /// <summary>
    /// Marks every port as not initialized and refreshes it, so the renderer measures its position and size again.
    /// </summary>
    public void ReinitializePorts()
    {
        foreach (var port in Ports)
        {
            port.Initialized = false;
            port.Refresh();
        }
    }

    #endregion

    /// <summary>
    /// Moves the node, moves its ports by the same offset, refreshes the node and its links and raises <see
    /// cref="Moving"/>.
    /// </summary>
    public override void SetPosition(double x, double y)
    {
        var deltaX = x - Position.X;
        var deltaY = y - Position.Y;
        base.SetPosition(x, y);

        UpdatePortPositions(deltaX, deltaY);
        Refresh();
        RefreshLinks();
        Moving?.Invoke(this);
    }

    /// <summary>
    /// Moves the node and its ports by the given offset in diagram units and refreshes the node, but does not refresh
    /// links or raise <see cref="Moving"/>. Used when a group moves its children.
    /// </summary>
    public virtual void UpdatePositionSilently(double deltaX, double deltaY)
    {
        base.SetPosition(Position.X + deltaX, Position.Y + deltaY);
        UpdatePortPositions(deltaX, deltaY);
        Refresh();
    }

    /// <summary>The node's rectangle, or <c>null</c> while <see cref="Size"/> is unknown.</summary>
    public Rectangle? GetBounds() => GetBounds(false);

    /// <summary>
    /// The node's rectangle, extended when <paramref name="includePorts"/> is <c>true</c> to cover the ports aligned
    /// to its left, top, right and bottom sides; corner ports are ignored. <c>null</c> while <see cref="Size"/> is
    /// unknown.
    /// </summary>
    public Rectangle? GetBounds(bool includePorts)
    {
        if (Size == null)
            return null;

        if (!includePorts)
            return new Rectangle(Position, Size);

        var leftPort = GetPort(PortAlignment.Left);
        var topPort = GetPort(PortAlignment.Top);
        var rightPort = GetPort(PortAlignment.Right);
        var bottomPort = GetPort(PortAlignment.Bottom);

        var left = leftPort == null ? Position.X : Math.Min(Position.X, leftPort.Position.X);
        var top = topPort == null ? Position.Y : Math.Min(Position.Y, topPort.Position.Y);
        var right = rightPort == null
            ? Position.X + Size!.Width
            : Math.Max(rightPort.Position.X + rightPort.Size.Width, Position.X + Size!.Width);
        var bottom = bottomPort == null
            ? Position.Y + Size!.Height
            : Math.Max(bottomPort.Position.Y + bottomPort.Size.Height, Position.Y + Size!.Height);

        return new Rectangle(left, top, right, bottom);
    }

    /// <summary>A rectangle at the node's position and size; override for another outline.</summary>
    public virtual IShape GetShape() => Shapes.Rectangle(this);

    /// <summary>A link starting on a node may end on any model except a port, a link or a locked model.</summary>
    public virtual bool CanAttachTo(ILinkable other) =>
        other is not PortModel && other is not BaseLinkModel && other is not Model { Locked: true };

    private void UpdatePortPositions(double deltaX, double deltaY)
    {
        // Save some JS calls and update ports directly here
        foreach (var port in _ports)
        {
            port.Position = new Point(port.Position.X + deltaX, port.Position.Y + deltaY);
            port.RefreshLinks();
        }
    }

    /// <summary>Raises <see cref="Moving"/>, for subclasses that change the position without calling <see cref="SetPosition"/>.</summary>
    protected void TriggerMoving()
    {
        Moving?.Invoke(this);
    }

    void ILinkable.AddLink(BaseLinkModel link) => _links.Add(link);

    void ILinkable.RemoveLink(BaseLinkModel link) => _links.Remove(link);
}