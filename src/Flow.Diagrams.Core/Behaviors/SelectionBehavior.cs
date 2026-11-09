using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Events;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>Selects on pointer down: a model replaces the selection, Ctrl adds it (when <see cref="Options.DiagramOptions.AllowMultiSelection"/> is on) or removes it if already selected, and the empty background clears the selection. Registered by default.</summary>
public class SelectionBehavior : Behavior
{
    /// <summary>Starts listening to pointer events of <paramref name="diagram"/>.</summary>
    public SelectionBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.PointerDown += OnPointerDown;
    }

    private void OnPointerDown(Model? model, PointerEventArgs e)
    {
        var ctrlKey = e.CtrlKey;
        switch (model)
        {
            case null:
                Diagram.UnselectAll();
                break;
            case SelectableModel sm when ctrlKey && sm.Selected:
                Diagram.UnselectModel(sm);
                break;
            case SelectableModel sm:
            {
                if (!sm.Selected)
                {
                    Diagram.SelectModel(sm, !ctrlKey || !Diagram.Options.AllowMultiSelection);
                }

                break;
            }
        }
    }

    /// <summary>Stops listening to pointer events.</summary>
    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;
    }
}
