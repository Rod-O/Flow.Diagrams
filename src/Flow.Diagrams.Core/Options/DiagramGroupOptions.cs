using Flow.Diagrams.Core.Models;

namespace Flow.Diagrams.Core.Options;

/// <summary>Options for grouping nodes.</summary>
public class DiagramGroupOptions
{
    /// <summary>
    /// Whether the keyboard shortcut that groups the selected nodes (or ungroups them if any is already in a group)
    /// is active; <c>false</c> by default. <see cref="Layers.GroupLayer.Group"/> does not need it.
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Creates the group in <see cref="Layers.GroupLayer.Group"/>, which the grouping shortcut also uses. Creates a
    /// plain <see cref="GroupModel"/> by default.
    /// </summary>
    public GroupFactory Factory { get; set; } = (diagram, children) => new GroupModel(children);
}