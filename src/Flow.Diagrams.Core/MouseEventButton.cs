namespace Flow.Diagrams.Core;

/// <summary>
/// Values of the DOM <c>button</c> property, to compare with <see cref="Events.MouseEventArgs.Button"/> (a
/// <c>long</c>).
/// </summary>
public enum MouseEventButton : long
{
    /// <summary>The main (usually left) button.</summary>
    Left = 0,
    /// <summary>The wheel or middle button.</summary>
    Wheel = 1,
    /// <summary>The secondary (usually right) button.</summary>
    Right = 2,
    /// <summary>The fourth button, usually Back.</summary>
    Fourth = 4,
    /// <summary>The fifth button, usually Forward.</summary>
    Fifth = 5
}
