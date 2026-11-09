using Flow.Diagrams.Core.Controls;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Site.Models.Controls;

public class NodeInformationControl : Control
{
    public override Point? GetPosition(Model model)
    {
        // We want the information to be under the node
        var node = (model as NodeModel)!;
        if (node.Size == null)
            return null;

        return node.Position.Add(0, node.Size!.Height + 10);
    }
}
