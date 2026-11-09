using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Controls;

/// <summary>Shows and hides each model's <see cref="ControlsContainer"/> according to its <see cref="ControlsContainer.Type"/>: on pointer enter and leave for <see cref="ControlsType.OnHover"/>, on selection and deselection for <see cref="ControlsType.OnSelection"/>. Registered by default.</summary>
public class ControlsBehavior : Behavior
{
    /// <summary>Starts listening to pointer and selection events of <paramref name="diagram"/>.</summary>
    public ControlsBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.PointerEnter += OnPointerEnter;
        Diagram.PointerLeave += OnPointerLeave;
        Diagram.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(SelectableModel model)
    {
        var controls = Diagram.Controls.GetFor(model);
        if (controls is not { Type: ControlsType.OnSelection })
            return;

        if (model.Selected)
        {
            controls.Show();
        }
        else
        {
            controls.Hide();
        }
    }

    private void OnPointerEnter(Model? model, PointerEventArgs e)
    {
        if (model == null)
            return;
        
        var controls = Diagram.Controls.GetFor(model);
        if (controls is not { Type: ControlsType.OnHover })
            return;
        
        controls.Show();
    }

    private void OnPointerLeave(Model? model, PointerEventArgs e)
    {
        if (model == null)
            return;
        
        var controls = Diagram.Controls.GetFor(model);
        if (controls is not { Type: ControlsType.OnHover })
            return;
        
        controls.Hide();
    }

    /// <summary>Stops listening to diagram events.</summary>
    public override void Dispose()
    {
        Diagram.PointerEnter -= OnPointerEnter;
        Diagram.PointerLeave -= OnPointerLeave;
        Diagram.SelectionChanged -= OnSelectionChanged;
    }
}