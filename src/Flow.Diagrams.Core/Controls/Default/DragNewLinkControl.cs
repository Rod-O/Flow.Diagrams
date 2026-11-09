using System.Threading.Tasks;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Positions;

namespace Flow.Diagrams.Core.Controls.Default;

public class DragNewLinkControl : ExecutableControl
{
    private readonly IPositionProvider _positionProvider;

    public DragNewLinkControl(double x, double y, double offsetX = 0, double offsetY = 0)
        : this(new BoundsBasedPositionProvider(x, y, offsetX, offsetY))
    {
    }

    public DragNewLinkControl(IPositionProvider positionProvider)
    {
        _positionProvider = positionProvider;
    }

    public override Point? GetPosition(Model model) => _positionProvider.GetPosition(model);

    public override ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e)
    {
        if (model is not NodeModel node || node.Locked)
            return ValueTask.CompletedTask;
        
        var behavior = diagram.GetBehavior<DragNewLinkBehavior>();
        if (behavior == null)
            throw new DiagramsException($"DragNewLinkBehavior was not found");

        behavior.StartFrom(node, e.ClientX, e.ClientY);
        return ValueTask.CompletedTask;
    }
}