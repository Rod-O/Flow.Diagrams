namespace Flow.Diagrams.Core.Events;

/// <summary>A mouse wheel event, as reported by the browser.</summary>
/// <param name="ClientX">Horizontal position in client (viewport) pixels.</param>
/// <param name="ClientY">Vertical position in client (viewport) pixels.</param>
/// <param name="Button">The DOM <c>button</c> that changed; see <see cref="MouseEventButton"/>.</param>
/// <param name="Buttons">The DOM <c>buttons</c> bit mask of all buttons currently held.</param>
/// <param name="CtrlKey">Whether Ctrl was held.</param>
/// <param name="ShiftKey">Whether Shift was held.</param>
/// <param name="AltKey">Whether Alt was held.</param>
/// <param name="DeltaX">Horizontal scroll amount, in units of <paramref name="DeltaMode"/>.</param>
/// <param name="DeltaY">Vertical scroll amount; positive when scrolling down, in units of <paramref name="DeltaMode"/>.</param>
/// <param name="DeltaZ">Z-axis scroll amount, in units of <paramref name="DeltaMode"/>.</param>
/// <param name="DeltaMode">The DOM <c>deltaMode</c>: 0 pixels, 1 lines, 2 pages.</param>
public record WheelEventArgs(
    double ClientX,
    double ClientY,
    long Button,
    long Buttons,
    bool CtrlKey,
    bool ShiftKey,
    bool AltKey,
    double DeltaX,
    double DeltaY,
    double DeltaZ,
    long DeltaMode) : MouseEventArgs(ClientX, ClientY, Button, Buttons, CtrlKey, ShiftKey, AltKey);
