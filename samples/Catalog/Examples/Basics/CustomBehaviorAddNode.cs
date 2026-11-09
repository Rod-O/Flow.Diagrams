using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Catalog.Examples.Basics;

#region behavior
/// <summary>Double-clicking the empty canvas adds a node where the pointer is.</summary>
public class CustomBehaviorAddNode : Behavior
{
    public CustomBehaviorAddNode(Diagram diagram) : base(diagram)
    {
        Diagram.PointerDoubleClick += OnPointerDoubleClick;
    }

    private void OnPointerDoubleClick(Model? model, PointerEventArgs e)
    {
        if (model != null)
            return;

        var position = Diagram.GetRelativeMousePoint(e.ClientX, e.ClientY);
        var node = Diagram.Nodes.Add(new NodeModel(position) { Title = $"Node {Diagram.Nodes.Count + 1}" });
        node.AddPort(PortAlignment.Left);
        node.AddPort(PortAlignment.Right);
    }

    public override void Dispose()
    {
        Diagram.PointerDoubleClick -= OnPointerDoubleClick;
    }
}
#endregion
