using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Events;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>Pans the diagram by dragging the empty background with the left button, unless Shift is held or <see cref="Options.DiagramOptions.AllowPanning"/> is off. Registered by default.</summary>
public class PanBehavior : Behavior
{
    private Point? _initialPan;
    private double _lastClientX;
    private double _lastClientY;

    /// <summary>Starts listening to pointer events of <paramref name="diagram"/>.</summary>
    public PanBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.PointerDown += OnPointerDown;
        Diagram.PointerMove += OnPointerMove;
        Diagram.PointerUp += OnPointerUp;
    }

    private void OnPointerDown(Model? model, PointerEventArgs e)
    {
        if (e.Button != (int)MouseEventButton.Left)
            return;

        Start(model, e.ClientX, e.ClientY, e.ShiftKey);
    }

    private void OnPointerMove(Model? model, PointerEventArgs e) => Move(e.ClientX, e.ClientY);

    private void OnPointerUp(Model? model, PointerEventArgs e) => End();

    private void Start(Model? model, double clientX, double clientY, bool shiftKey)
    {
        if (!Diagram.Options.AllowPanning || model != null || shiftKey)
            return;

        _initialPan = Diagram.Pan;
        _lastClientX = clientX;
        _lastClientY = clientY;
    }

    private void Move(double clientX, double clientY)
    {
        if (!Diagram.Options.AllowPanning || _initialPan == null)
            return;

        var deltaX = clientX - _lastClientX - (Diagram.Pan.X - _initialPan.X);
        var deltaY = clientY - _lastClientY - (Diagram.Pan.Y - _initialPan.Y);
        Diagram.UpdatePan(deltaX, deltaY);
    }

    private void End()
    {
        if (!Diagram.Options.AllowPanning)
            return;

        _initialPan = null;
    }

    /// <summary>Stops listening to pointer events.</summary>
    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;
        Diagram.PointerMove -= OnPointerMove;
        Diagram.PointerUp -= OnPointerUp;
    }
}
