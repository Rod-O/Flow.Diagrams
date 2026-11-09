using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Runs an action when a key combination is pressed on the diagram. Starts with <c>Delete</c> for
/// <see cref="KeyboardShortcutsDefaults.DeleteSelection"/> and <c>Ctrl+Alt+g</c> for <see cref="KeyboardShortcutsDefaults.Grouping"/>;
/// change them with <see cref="SetShortcut"/> and <see cref="RemoveShortcut"/>. Registered by default.
/// </summary>
public class KeyboardShortcutsBehavior : Behavior
{
    private readonly Dictionary<string, Func<Diagram, ValueTask>> _shortcuts;

    /// <summary>Registers the default shortcuts and starts listening to key events of <paramref name="diagram"/>.</summary>
    public KeyboardShortcutsBehavior(Diagram diagram) : base(diagram)
    {
        _shortcuts = new Dictionary<string, Func<Diagram, ValueTask>>();
        SetShortcut("Delete", false, false, false, KeyboardShortcutsDefaults.DeleteSelection);
        SetShortcut("g", true, false, true, KeyboardShortcutsDefaults.Grouping);

        Diagram.KeyDown += OnDiagramKeyDown;
    }

    /// <summary>Binds <paramref name="action"/> to a key combination, replacing any action already bound to it.</summary>
    /// <param name="key">The key as reported by the browser's <c>KeyboardEvent.key</c>, compared case-sensitively (<c>"g"</c>, <c>"Delete"</c>).</param>
    /// <param name="ctrl">Whether Ctrl must be held.</param>
    /// <param name="shift">Whether Shift must be held.</param>
    /// <param name="alt">Whether Alt must be held.</param>
    /// <param name="action">Receives the diagram; runs when the combination is pressed.</param>
    public void SetShortcut(string key, bool ctrl, bool shift, bool alt, Func<Diagram, ValueTask> action)
    {
        var k = KeysUtils.GetStringRepresentation(ctrl, shift, alt, key);
        _shortcuts[k] = action;
    }

    /// <summary>Removes the action bound to a key combination, as described for <see cref="SetShortcut"/>; false when none was bound.</summary>
    public bool RemoveShortcut(string key, bool ctrl, bool shift, bool alt)
    {
        var k = KeysUtils.GetStringRepresentation(ctrl, shift, alt, key);
        return _shortcuts.Remove(k);
    }

    private async void OnDiagramKeyDown(KeyboardEventArgs e)
    {
        var k = KeysUtils.GetStringRepresentation(e.CtrlKey, e.ShiftKey, e.AltKey, e.Key);
        if (_shortcuts.TryGetValue(k, out var action))
        {
            await action(Diagram);
        }
    }

    /// <summary>Stops listening to key events.</summary>
    public override void Dispose()
    {
        Diagram.KeyDown -= OnDiagramKeyDown;
    }
}
