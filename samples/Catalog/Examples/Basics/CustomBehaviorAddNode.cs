using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Catalog.Examples.Basics;

#region behavior
/// <summary>Double-clicking the empty canvas adds a node where the pointer is.</summary>
public class CustomBehaviorAddNode : Behavior
{
    private bool _previousClickOnCanvas;

    public CustomBehaviorAddNode(Diagram diagram) : base(diagram)
    {
        Diagram.PointerClick += OnPointerClick;
        Diagram.PointerDoubleClick += OnPointerDoubleClick;
    }

    // Any two clicks within 500 ms make a double click, even on different models, so both clicks are checked.
    // EventsBehavior raises the double click before this handler records the second click.
    private void OnPointerClick(Model? model, PointerEventArgs e) => _previousClickOnCanvas = model == null;

    private void OnPointerDoubleClick(Model? model, PointerEventArgs e)
    {
        if (model != null || !_previousClickOnCanvas)
            return;

        var position = Diagram.GetRelativeMousePoint(e.ClientX, e.ClientY);
        var node = Diagram.Nodes.Add(new NodeModel(position) { Title = $"Node {Diagram.Nodes.Count + 1}" });
        node.AddPort(PortAlignment.Left);
        node.AddPort(PortAlignment.Right);
    }

    public override void Dispose()
    {
        Diagram.PointerClick -= OnPointerClick;
        Diagram.PointerDoubleClick -= OnPointerDoubleClick;
    }
}
#endregion
