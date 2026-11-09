using Flow.Diagrams.Core.Models;
using System.Linq;

namespace Flow.Diagrams.Core.Layers;

/// <summary>The groups of a diagram.</summary>
public class GroupLayer : BaseLayer<GroupModel>
{
    /// <summary>Creates the layer for <paramref name="diagram"/>.</summary>
    public GroupLayer(Diagram diagram) : base(diagram)
    {
    }

    /// <summary>
    /// Creates a group around <paramref name="children"/> with <see cref="Options.DiagramGroupOptions.Factory"/>,
    /// adds it and returns it.
    /// </summary>
    public GroupModel Group(params NodeModel[] children)
    {
        return Add(Diagram.Options.Groups.Factory(Diagram, children));
    }

    /// <summary>
    /// Removes the group AND its children
    /// </summary>
    public void Delete(GroupModel group)
    {
        Diagram.Batch(() =>
        {
            var children = group.Children.ToArray();

            Remove(group);

            foreach (var child in children)
            {
                if (child is GroupModel g)
                {
                    Delete(g);
                }
                else
                {
                    Diagram.Nodes.Remove(child);
                }
            }
        });
    }

    /// <summary>
    /// Also removes the links attached to the group and its ports, releases its children (they stay in the diagram),
    /// detaches the group from its own parent group and removes its controls.
    /// </summary>
    protected override void OnItemRemoved(GroupModel group)
    {
        Diagram.Links.Remove(group.PortLinks.ToArray());
        Diagram.Links.Remove(group.Links.ToArray());
        group.Ungroup();
        group.Group?.RemoveChild(group);
        Diagram.Controls.RemoveFor(group);
    }
}
