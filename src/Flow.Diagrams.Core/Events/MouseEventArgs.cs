namespace Flow.Diagrams.Core.Events;

/// <summary>
/// A mouse event, as reported by the browser; positions are client pixels, not diagram units (see <see
/// cref="Diagram.GetRelativeMousePoint"/>).
/// </summary>
/// <param name="ClientX">Horizontal position in client (viewport) pixels.</param>
/// <param name="ClientY">Vertical position in client (viewport) pixels.</param>
/// <param name="Button">The DOM <c>button</c> that changed; see <see cref="MouseEventButton"/>.</param>
/// <param name="Buttons">The DOM <c>buttons</c> bit mask of all buttons currently held.</param>
/// <param name="CtrlKey">Whether Ctrl was held.</param>
/// <param name="ShiftKey">Whether Shift was held.</param>
/// <param name="AltKey">Whether Alt was held.</param>
public record MouseEventArgs(double ClientX, double ClientY, long Button, long Buttons, bool CtrlKey, bool ShiftKey, bool AltKey);
