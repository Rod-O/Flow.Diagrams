using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using System.Collections.Generic;

namespace Flow.Diagrams.Core.Models;

/// <summary>
/// A connection point on a <see cref="NodeModel"/>. The renderer measures its position and size from the rendered
/// element, so both are only meaningful once <see cref="Initialized"/> is <c>true</c>.
/// </summary>
public class PortModel : Model, IHasBounds, IHasShape, ILinkable
{
    private readonly List<BaseLinkModel> _links = new(4);

    /// <summary>
    /// Creates a port of <paramref name="parent"/> on the given side. <paramref name="position"/> and <paramref
    /// name="size"/> default to zero until the renderer measures them. It is not added to the node; call <see
    /// cref="NodeModel.AddPort(PortModel)"/>.
    /// </summary>
    public PortModel(NodeModel parent, PortAlignment alignment = PortAlignment.Bottom, Point? position = null,
        Size? size = null)
    {
        Parent = parent;
        Alignment = alignment;
        Position = position ?? Point.Zero;
        Size = size ?? Size.Zero;
    }

    /// <summary>
    /// Creates a port with the given <paramref name="id"/>; otherwise like the constructor without an id.
    /// </summary>
    public PortModel(string id, NodeModel parent, PortAlignment alignment = PortAlignment.Bottom,
        Point? position = null, Size? size = null) : base(id)
    {
        Parent = parent;
        Alignment = alignment;
        Position = position ?? Point.Zero;
        Size = size ?? Size.Zero;
    }

    /// <summary>The node the port belongs to.</summary>
    public NodeModel Parent { get; }
    /// <summary>
    /// The side or corner of the node the port sits on; routers and anchors use it to leave the node in the right
    /// direction.
    /// </summary>
    public PortAlignment Alignment { get; }
    /// <summary>Top-left corner of the port in diagram units; moved together with the node.</summary>
    public Point Position { get; set; }
    /// <summary>Center of the port in diagram units.</summary>
    public Point MiddlePosition => new(Position.X + (Size.Width / 2), Position.Y + (Size.Height / 2));
    /// <summary>Size of the port in diagram units; zero until measured.</summary>
    public Size Size { get; set; }
    /// <summary>The links attached to this port.</summary>
    public IReadOnlyList<BaseLinkModel> Links => _links;
    /// <summary>
    /// If set to false, a call to Refresh() will force the port to update its position/size
    /// </summary>
    public bool Initialized { get; set; }

    /// <summary>Refreshes the port, then its links.</summary>
    public void RefreshAll()
    {
        Refresh();
        RefreshLinks();
    }

    /// <summary>Refreshes the links attached to this port and the links attached to those.</summary>
    public void RefreshLinks()
    {
        foreach (var link in Links)
        {
            link.Refresh();
            link.RefreshLinks();
        }
    }

    /// <summary>
    /// The parent node cast to <typeparamref name="T"/>; throws <see cref="InvalidCastException"/> if it is another
    /// type.
    /// </summary>
    public T GetParent<T>() where T : NodeModel => (T)Parent;

    /// <summary>The port's rectangle in diagram units.</summary>
    public Rectangle GetBounds() => new(Position, Size);

    /// <summary>A circle at the port's position and size; override for another outline.</summary>
    public virtual IShape GetShape() => Shapes.Circle(this);

    /// <summary>
    /// A link starting on a port may end on another port that is not this one, belongs to a different node, and is not
    /// locked, nor is its node.
    /// </summary>
    public virtual bool CanAttachTo(ILinkable other)
    {
        // Todo: remove in order to support same node links
        return other is PortModel port && port != this && !port.Locked && !port.Parent.Locked && Parent != port.Parent;
    }

    void ILinkable.AddLink(BaseLinkModel link) => _links.Add(link);

    void ILinkable.RemoveLink(BaseLinkModel link) => _links.Remove(link);
}
