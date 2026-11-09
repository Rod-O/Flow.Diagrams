namespace Flow.Diagrams.Core.Events;

/// <summary>A pointer event (mouse, pen or touch), as reported by the browser.</summary>
/// <param name="ClientX">Horizontal position in client (viewport) pixels.</param>
/// <param name="ClientY">Vertical position in client (viewport) pixels.</param>
/// <param name="Button">The DOM <c>button</c> that changed; see <see cref="MouseEventButton"/>.</param>
/// <param name="Buttons">The DOM <c>buttons</c> bit mask of all buttons currently held.</param>
/// <param name="CtrlKey">Whether Ctrl was held.</param>
/// <param name="ShiftKey">Whether Shift was held.</param>
/// <param name="AltKey">Whether Alt was held.</param>
/// <param name="PointerId">The DOM <c>pointerId</c>.</param>
/// <param name="Width">Contact width in CSS pixels.</param>
/// <param name="Height">Contact height in CSS pixels.</param>
/// <param name="Pressure">Normalized pressure, 0 to 1.</param>
/// <param name="TiltX">Pen tilt around the Y axis, in degrees.</param>
/// <param name="TiltY">Pen tilt around the X axis, in degrees.</param>
/// <param name="PointerType">The DOM <c>pointerType</c>: <c>"mouse"</c>, <c>"pen"</c> or <c>"touch"</c>.</param>
/// <param name="IsPrimary">Whether this is the primary pointer of its type.</param>
public record PointerEventArgs(double ClientX, double ClientY, long Button, long Buttons, bool CtrlKey, bool ShiftKey,
    bool AltKey, long PointerId, float Width, float Height, float Pressure, float TiltX, float TiltY,
    string PointerType, bool IsPrimary) : MouseEventArgs(ClientX, ClientY, Button, Buttons, CtrlKey, ShiftKey, AltKey);