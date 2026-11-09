using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Runs an action when a key combination is pressed on the diagram. Starts with <c>Delete</c> for
/// <see cref="KeyboardShortcutsDefaults.DeleteSelection"/> and <c>Ctrl+Alt+g</c> for <see cref="KeyboardShortcutsDefaults.Grouping"/>;
/// change them with <see cref="SetShortcut(string, bool, bool, bool, bool, Func{Diagram, ValueTask})"/> and
/// <see cref="RemoveShortcut(string, bool, bool, bool, bool)"/>. Registered by default.
/// </summary>
/// <remarks>
/// A shortcut is matched on the character the key types, ignoring case. When that character is not a letter or digit,
/// as when Option on macOS turns Option+G into <c>©</c>, it is matched on the physical key instead (<c>KeyG</c> as
/// <c>g</c>, <c>Digit1</c> as <c>1</c>), so the default <c>Ctrl+Alt+g</c> also works on macOS. Shift+1 typing <c>!</c>
/// therefore matches a shortcut on <c>"1"</c> with Shift when none is bound to <c>"!"</c>.
/// </remarks>
public class KeyboardShortcutsBehavior : Behavior
{
    private readonly Dictionary<string, Func<Diagram, ValueTask>> _shortcuts;

    /// <summary>Registers the default shortcuts and starts listening to key events of <paramref name="diagram"/>.</summary>
    public KeyboardShortcutsBehavior(Diagram diagram) : base(diagram)
    {
        _shortcuts = new Dictionary<string, Func<Diagram, ValueTask>>(StringComparer.OrdinalIgnoreCase);
        SetShortcut("Delete", false, false, false, KeyboardShortcutsDefaults.DeleteSelection);
        SetShortcut("g", true, false, true, KeyboardShortcutsDefaults.Grouping);

        Diagram.KeyDown += OnDiagramKeyDown;
    }

    /// <summary>
    /// Raised when a shortcut's action throws, with the exception. Without a handler the exception is rethrown, which
    /// in Blazor reaches the renderer as an unhandled error.
    /// </summary>
    public event Action<Exception>? ActionFailed;

    /// <summary>Binds <paramref name="action"/> to a key combination without Meta, replacing any action already bound to it.</summary>
    /// <param name="key">The key as reported by the browser's <c>KeyboardEvent.key</c> (<c>"g"</c>, <c>"Delete"</c>), compared
    /// case-insensitively, so <c>"a"</c> with <paramref name="shift"/> matches the <c>"A"</c> the browser reports.</param>
    /// <param name="ctrl">Whether Ctrl must be held.</param>
    /// <param name="shift">Whether Shift must be held.</param>
    /// <param name="alt">Whether Alt (Option on macOS) must be held.</param>
    /// <param name="action">Receives the diagram; runs when the combination is pressed.</param>
    public void SetShortcut(string key, bool ctrl, bool shift, bool alt, Func<Diagram, ValueTask> action)
        => SetShortcut(key, ctrl, shift, alt, false, action);

    /// <summary>Binds <paramref name="action"/> to a key combination, replacing any action already bound to it.</summary>
    /// <param name="key">The key as reported by the browser's <c>KeyboardEvent.key</c> (<c>"g"</c>, <c>"Delete"</c>), compared
    /// case-insensitively, so <c>"a"</c> with <paramref name="shift"/> matches the <c>"A"</c> the browser reports.</param>
    /// <param name="ctrl">Whether Ctrl must be held.</param>
    /// <param name="shift">Whether Shift must be held.</param>
    /// <param name="alt">Whether Alt (Option on macOS) must be held.</param>
    /// <param name="meta">Whether Meta (Cmd on macOS) must be held.</param>
    /// <param name="action">Receives the diagram; runs when the combination is pressed.</param>
    public void SetShortcut(string key, bool ctrl, bool shift, bool alt, bool meta, Func<Diagram, ValueTask> action)
    {
        var k = KeysUtils.GetStringRepresentation(ctrl, shift, alt, meta, key);
        _shortcuts[k] = action;
    }

    /// <summary>Removes the action bound to a key combination without Meta, as described for <see cref="SetShortcut(string, bool, bool, bool, Func{Diagram, ValueTask})"/>; false when none was bound.</summary>
    public bool RemoveShortcut(string key, bool ctrl, bool shift, bool alt)
        => RemoveShortcut(key, ctrl, shift, alt, false);

    /// <summary>Removes the action bound to a key combination, as described for <see cref="SetShortcut(string, bool, bool, bool, bool, Func{Diagram, ValueTask})"/>; false when none was bound.</summary>
    public bool RemoveShortcut(string key, bool ctrl, bool shift, bool alt, bool meta)
    {
        var k = KeysUtils.GetStringRepresentation(ctrl, shift, alt, meta, key);
        return _shortcuts.Remove(k);
    }

    private async void OnDiagramKeyDown(KeyboardEventArgs e)
    {
        var k = KeysUtils.GetStringRepresentation(e.CtrlKey, e.ShiftKey, e.AltKey, e.MetaKey, e.Key);
        if (!_shortcuts.TryGetValue(k, out var action))
        {
            var physical = GetPhysicalCharacter(e);
            if (physical == null)
                return;

            k = KeysUtils.GetStringRepresentation(e.CtrlKey, e.ShiftKey, e.AltKey, e.MetaKey, physical);
            if (!_shortcuts.TryGetValue(k, out action))
                return;
        }

        try
        {
            await action(Diagram);
        }
        catch (Exception ex) when (ActionFailed != null)
        {
            ActionFailed(ex);
        }
    }

    // Option on macOS (and AltGr elsewhere) turns Option+G into "©"; fall back to the physical key, but only when the
    // key is not itself a letter or digit, so that layouts like AZERTY keep matching on the character they type.
    private static string? GetPhysicalCharacter(KeyboardEventArgs e)
    {
        if (e.Key.Length == 1 && char.IsAsciiLetterOrDigit(e.Key[0]))
            return null;

        if (e.Code.Length == 4 && e.Code.StartsWith("Key", StringComparison.Ordinal))
            return e.Code.Substring(3);

        if (e.Code.Length == 6 && e.Code.StartsWith("Digit", StringComparison.Ordinal))
            return e.Code.Substring(5);

        return null;
    }

    /// <summary>Stops listening to key events.</summary>
    public override void Dispose()
    {
        Diagram.KeyDown -= OnDiagramKeyDown;
    }
}
