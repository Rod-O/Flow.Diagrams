using System.Threading.Tasks;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Positions;

namespace Flow.Diagrams.Core.Controls.Default;

/// <summary>A delete button for a group, node or link. Honors <see cref="Models.Base.Model.Locked"/> and the <see cref="Options.DiagramOptions.Constraints"/> delete checks.</summary>
public class RemoveControl : ExecutableControl
{
    private readonly IPositionProvider _positionProvider;

    /// <summary>Creates a button at fractions <paramref name="x"/> and <paramref name="y"/> of the model's bounds, shifted by the offsets; see <see cref="BoundsBasedPositionProvider"/>.</summary>
    public RemoveControl(double x, double y, double offsetX = 0, double offsetY = 0)
        : this(new BoundsBasedPositionProvider(x, y, offsetX, offsetY))
    {
    }

    /// <summary>Creates a button placed by <paramref name="positionProvider"/>.</summary>
    public RemoveControl(IPositionProvider positionProvider)
    {
        _positionProvider = positionProvider;
    }

    /// <inheritdoc />
    public override Point? GetPosition(Model model) => _positionProvider.GetPosition(model);

    /// <summary>Deletes the model when it is not locked and the matching <c>ShouldDelete*</c> constraint returns true; other kinds of model are ignored.</summary>
    public override async ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs _)
    {
        if (await ShouldDeleteModel(diagram, model))
        {
            DeleteModel(diagram, model);
        }
    }

    private static void DeleteModel(Diagram diagram, Model model)
    {
       switch (model)
        {
            case GroupModel group:
                diagram.Groups.Delete(group);
                return;
            case NodeModel node:
                diagram.Nodes.Remove(node);
                return;

            case BaseLinkModel link:
                diagram.Links.Remove(link);
                return;
        }
    }

    private static async ValueTask<bool> ShouldDeleteModel(Diagram diagram, Model model)
    {
        if (model.Locked)
        {
            return false;
        }

        return model switch
        {
            GroupModel group => await diagram.Options.Constraints.ShouldDeleteGroup.Invoke(group),
            NodeModel node => await diagram.Options.Constraints.ShouldDeleteNode.Invoke(node),
            BaseLinkModel link => await diagram.Options.Constraints.ShouldDeleteLink.Invoke(link),
            _ => false,
        };
    }
}