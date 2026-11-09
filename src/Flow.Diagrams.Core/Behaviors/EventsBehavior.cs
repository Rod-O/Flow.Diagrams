using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Events;
using System.Diagnostics;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Turns raw pointer events into the diagram's click and double-click events: a press and release on the same model with no
/// move in between raises <see cref="Diagram.PointerClick"/>, and a second click on the same model (or on the canvas twice)
/// within 500 ms of the previous one also raises <see cref="Diagram.PointerDoubleClick"/>. The click that completes a
/// double click starts no new one, so a third quick click is a single click. Registered by default.
/// </summary>
public class EventsBehavior : Behavior
{
    private readonly Stopwatch _mouseClickSw;
    private Model? _model;
    private Model? _lastClickedModel;
    private bool _captureMouseMove;
    private int _mouseMovedCount;

    /// <summary>Starts listening to pointer events and model removals of <paramref name="diagram"/>.</summary>
    public EventsBehavior(Diagram diagram) : base(diagram)
    {
        _mouseClickSw = new Stopwatch();

        Diagram.PointerDown += OnPointerDown;
        Diagram.PointerMove += OnPointerMove;
        Diagram.PointerUp += OnPointerUp;
        Diagram.PointerClick += OnPointerClick;
        Diagram.Nodes.Removed += OnModelRemoved;
        Diagram.Links.Removed += OnModelRemoved;
        Diagram.Groups.Removed += OnModelRemoved;
    }

    // Forgets a removed model, which would otherwise stay reachable until the next press or click.
    private void OnModelRemoved(Model model)
    {
        if (_model == model)
            _model = null;

        if (_lastClickedModel == model)
        {
            _lastClickedModel = null;
            _mouseClickSw.Reset();
        }
    }

    private void OnPointerClick(Model? model, PointerEventArgs e)
    {
        if (_mouseClickSw.IsRunning && _mouseClickSw.ElapsedMilliseconds <= 500 && _lastClickedModel == model)
        {
            _mouseClickSw.Reset();
            _lastClickedModel = null;
            Diagram.TriggerPointerDoubleClick(model, e);
            return;
        }

        _lastClickedModel = model;
        _mouseClickSw.Restart();
    }

    private void OnPointerDown(Model? model, PointerEventArgs e)
    {
        _captureMouseMove = true;
        _mouseMovedCount = 0;
        _model = model;
    }

    private void OnPointerMove(Model? model, PointerEventArgs e)
    {
        if (!_captureMouseMove)
            return;

        _mouseMovedCount++;
    }

    private void OnPointerUp(Model? model, PointerEventArgs e)
    {
        if (!_captureMouseMove) return; // Only set by OnMouseDown
        _captureMouseMove = false;
        if (_mouseMovedCount > 0) return;

        if (_model == model)
        {
            Diagram.TriggerPointerClick(model, e);
            _model = null;
        }
    }

    /// <summary>Stops listening to pointer events and model removals.</summary>
    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;
        Diagram.PointerMove -= OnPointerMove;
        Diagram.PointerUp -= OnPointerUp;
        Diagram.PointerClick -= OnPointerClick;
        Diagram.Nodes.Removed -= OnModelRemoved;
        Diagram.Links.Removed -= OnModelRemoved;
        Diagram.Groups.Removed -= OnModelRemoved;
        _model = null;
        _lastClickedModel = null;
    }
}
