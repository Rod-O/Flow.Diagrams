namespace Blazor.Diagrams.Core.Models;

/// <summary>
/// Space between the bounds of a group's children and the group's edges, per side.
/// </summary>
public readonly record struct GroupPadding(double Left, double Top, double Right, double Bottom)
{
    /// <summary>The same space on every side.</summary>
    public GroupPadding(double all) : this(all, all, all, all)
    {
    }
}
