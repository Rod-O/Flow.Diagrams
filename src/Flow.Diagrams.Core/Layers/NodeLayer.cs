using Flow.Diagrams.Core.Models;
using System.Linq;

namespace Flow.Diagrams.Core.Layers;

/// <summary>The nodes of a diagram, excluding groups.</summary>
public class NodeLayer : BaseLayer<NodeModel>
{
    /// <summary>Creates the layer for <paramref name="diagram"/>.</summary>
    public NodeLayer(Diagram diagram) : base(diagram) { }

    /// <summary>
    /// Also removes the links attached to the node and its ports, takes the node out of its group and removes its
    /// controls.
    /// </summary>
    protected override void OnItemRemoved(NodeModel node)
    {
        Diagram.Links.Remove(node.PortLinks.ToList());
        Diagram.Links.Remove(node.Links.ToList());
        node.Group?.RemoveChild(node);
        Diagram.Controls.RemoveFor(node);
    }
}
