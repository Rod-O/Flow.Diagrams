namespace Flow.Diagrams.Core.Options;

/// <summary>
/// Hides models whose bounds are outside the viewport (sets <see cref="Models.Base.Model.Visible"/> to <c>false</c>) so they are
/// not rendered. Re-evaluated when the pan or container changes.
/// </summary>
public class DiagramVirtualizationOptions
{
    /// <summary>Master switch; <c>false</c> by default.</summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Whether nodes are virtualized; <c>true</c> by default, effective only when <see cref="Enabled"/>.
    /// </summary>
    public bool OnNodes { get; set; } = true;
    /// <summary>Whether groups are virtualized; <c>false</c> by default.</summary>
    public bool OnGroups { get; set; }
    /// <summary>Whether links are virtualized; <c>false</c> by default.</summary>
    public bool OnLinks { get; set; }
}