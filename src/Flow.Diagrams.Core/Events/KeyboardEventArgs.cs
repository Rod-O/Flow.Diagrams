namespace Flow.Diagrams.Core.Events;

/// <summary>A key press, as reported by the browser.</summary>
/// <param name="Key">The DOM <c>key</c> value, such as <c>"a"</c> or <c>"Delete"</c>.</param>
/// <param name="Code">The DOM <c>code</c> value, the physical key such as <c>"KeyA"</c>.</param>
/// <param name="Location">The DOM <c>location</c>: 0 standard, 1 left, 2 right, 3 numpad.</param>
/// <param name="CtrlKey">Whether Ctrl was held.</param>
/// <param name="ShiftKey">Whether Shift was held.</param>
/// <param name="AltKey">Whether Alt (Option on macOS) was held.</param>
/// <param name="MetaKey">Whether Meta (Cmd on macOS, the Windows key on Windows) was held.</param>
public record KeyboardEventArgs(string Key, string Code, float Location, bool CtrlKey, bool ShiftKey, bool AltKey, bool MetaKey = false);
