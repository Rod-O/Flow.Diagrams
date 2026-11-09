namespace Flow.Diagrams.Core.Events;

/// <summary>A touch event, as reported by the browser. Nothing in the library raises it.</summary>
/// <param name="ChangedTouches">The touch points that changed.</param>
/// <param name="CtrlKey">Whether Ctrl was held.</param>
/// <param name="ShiftKey">Whether Shift was held.</param>
/// <param name="AltKey">Whether Alt was held.</param>
public record TouchEventArgs(TouchPoint[] ChangedTouches, bool CtrlKey, bool ShiftKey, bool AltKey);
/// <summary>One touch contact.</summary>
/// <param name="Identifier">The DOM touch <c>identifier</c>, stable while the finger stays down.</param>
/// <param name="ClientX">Horizontal position in client pixels.</param>
/// <param name="ClientY">Vertical position in client pixels.</param>
public record TouchPoint(long Identifier, double ClientX, double ClientY);
