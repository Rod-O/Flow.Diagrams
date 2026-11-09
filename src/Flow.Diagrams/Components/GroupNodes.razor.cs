namespace Flow.Diagrams.Components;

/// <summary>Draws a group's children inside its widget, in the layer the group belongs to, ordered among themselves by their <see cref="Core.Models.Base.SelectableModel.Layer"/> and <see cref="Core.Models.Base.SelectableModel.Order"/>; nested groups become <see cref="Renderers.GroupRenderer"/>s and nodes <see cref="Renderers.NodeRenderer"/>s. The children are offset by the group's position, so they keep diagram coordinates.</summary>
public partial class GroupNodes { }
