using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Controls;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Catalog.Examples.Basics;

#region control
/// <summary>A button under a node that adds a linked child node below it.</summary>
public class ControlsAddChild : ExecutableControl
{
    public override Point? GetPosition(Model model) =>
        model is NodeModel { Size: { } size } node ? new Point(node.Position.X + size.Width / 2, node.Position.Y + size.Height + 18) : null;

    public override ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e)
    {
        if (model is not NodeModel parent)
            return ValueTask.CompletedTask;

        var child = diagram.Nodes.Add(new NodeModel(new Point(parent.Position.X, parent.Position.Y + 160)) { Title = "Child" });
        diagram.Links.Add(new LinkModel(parent.GetPort(PortAlignment.Bottom) ?? parent.AddPort(PortAlignment.Bottom), child.AddPort(PortAlignment.Top)));
        ControlsExample.AddNodeControls(diagram, child);
        return ValueTask.CompletedTask;
    }
}
#endregion
