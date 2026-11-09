using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Controls;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Models;

namespace Catalog.Examples.Basics;

public static class ControlsExample
{
    public static void AddNodeControls(Diagram diagram, NodeModel node)
    {
        #region node
        diagram.Controls.AddFor(node, ControlsType.OnSelection)
            .Add(new BoundaryControl())
            .Add(new RemoveControl(1, 0))
            .Add(new ControlsAddChild());
        #endregion
    }
}
