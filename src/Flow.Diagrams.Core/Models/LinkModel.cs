using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Models;

/// <summary>The standard link: a <see cref="BaseLinkModel"/> with a color and width.</summary>
public class LinkModel : BaseLinkModel
{
    /// <summary>Creates a link between two anchors.</summary>
    public LinkModel(Anchor source, Anchor target) : base(source, target) { }

    /// <summary>Creates a link with the given <paramref name="id"/> between two anchors.</summary>
    public LinkModel(string id, Anchor source, Anchor target) : base(id, source, target) { }

    /// <summary>Creates a link between two ports, each with a <see cref="SinglePortAnchor"/>.</summary>
    public LinkModel(PortModel sourcePort, PortModel targetPort)
        : base(new SinglePortAnchor(sourcePort), new SinglePortAnchor(targetPort)) { }

    /// <summary>
    /// Creates a link between two nodes, each with a <see cref="ShapeIntersectionAnchor"/>, so it ends where it
    /// crosses the node's outline.
    /// </summary>
    public LinkModel(NodeModel sourceNode, NodeModel targetNode)
        : base(new ShapeIntersectionAnchor(sourceNode), new ShapeIntersectionAnchor(targetNode)) { }

    /// <summary>Creates a link with the given <paramref name="id"/> between two ports.</summary>
    public LinkModel(string id, PortModel sourcePort, PortModel targetPort)
        : base(id, new SinglePortAnchor(sourcePort), new SinglePortAnchor(targetPort)) { }

    /// <summary>Creates a link with the given <paramref name="id"/> between two nodes.</summary>
    public LinkModel(string id, NodeModel sourceNode, NodeModel targetNode)
        : base(id, new ShapeIntersectionAnchor(sourceNode), new ShapeIntersectionAnchor(targetNode)) { }

    /// <summary>
    /// Stroke color as any SVG color string; <c>null</c> uses the Blazor options' default link color.
    /// </summary>
    public string? Color { get; set; }
    /// <summary>
    /// Stroke color while the link is selected; <c>null</c> uses the Blazor options' default selected color.
    /// </summary>
    public string? SelectedColor { get; set; }
    /// <summary>Stroke width in diagram units; 2 by default.</summary>
    public double Width { get; set; } = 2;
}
