using System.Text;

namespace Flow.Diagrams.Core.Utils;

/// <summary>Helpers for keyboard shortcuts.</summary>
public static class KeysUtils
{
    /// <summary>
    /// Builds the shortcut name used to look up keyboard shortcuts: modifiers in the order Ctrl, Shift, Alt joined by
    /// <c>+</c>, then the key, for example <c>Ctrl+Shift+z</c>.
    /// </summary>
    public static string GetStringRepresentation(bool ctrl, bool shift, bool alt, string key)
    {
        var sb = new StringBuilder();

        if (ctrl) sb.Append("Ctrl+");
        if (shift) sb.Append("Shift+");
        if (alt) sb.Append("Alt+");
        sb.Append(key);

        return sb.ToString();
    }
}
