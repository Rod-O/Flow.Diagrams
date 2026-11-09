using System.Threading.Tasks;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Positions;

namespace Flow.Diagrams.Core.Controls.Default;

/// <summary>A handle on a node that starts dragging a new link from it when pressed. Needs <see cref="DragNewLinkBehavior"/>.</summary>
public class DragNewLinkControl : ExecutableControl
{
    private readonly IPositionProvider _positionProvider;

    /// <summary>Creates a handle at fractions <paramref name="x"/> and <paramref name="y"/> of the node's bounds, shifted by the offsets; see <see cref="BoundsBasedPositionProvider"/>.</summary>
    public DragNewLinkControl(double x, double y, double offsetX = 0, double offsetY = 0)
        : this(new BoundsBasedPositionProvider(x, y, offsetX, offsetY))
    {
    }

    /// <summary>Creates a handle placed by <paramref name="positionProvider"/>.</summary>
    public DragNewLinkControl(IPositionProvider positionProvider)
    {
        _positionProvider = positionProvider;
    }

    /// <inheritdoc />
    public override Point? GetPosition(Model model) => _positionProvider.GetPosition(model);

    /// <summary>Starts a link from the node, unless <paramref name="model"/> is not a node or is locked. Throws <see cref="DiagramsException"/> when the diagram has no <see cref="DragNewLinkBehavior"/>.</summary>
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