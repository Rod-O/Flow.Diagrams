namespace Flow.Diagrams.Core.Models;

/// <summary>
/// Space between the bounds of a group's children and the group's edges, per side.
/// </summary>
/// <param name="Left">Space on the left side.</param>
/// <param name="Top">Space on the top side.</param>
/// <param name="Right">Space on the right side.</param>
/// <param name="Bottom">Space on the bottom side.</param>
public readonly record struct GroupPadding(double Left, double Top, double Right, double Bottom)
{
    /// <summary>The same space on every side.</summary>
    public GroupPadding(double all) : this(all, all, all, all)
    {
    }
}
