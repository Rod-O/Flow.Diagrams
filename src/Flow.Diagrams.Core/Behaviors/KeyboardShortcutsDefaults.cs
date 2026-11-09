using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using System.Linq;
using System.Threading.Tasks;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>The actions bound by default in <see cref="KeyboardShortcutsBehavior"/>, public so they can be rebound to other keys.</summary>
public static class KeyboardShortcutsDefaults
{
    /// <summary>Removes the selected groups, nodes and links that are not locked and that <see cref="Options.DiagramOptions.Constraints"/> allows deleting. Refreshes the diagram once at the end, also when a constraint throws (the exception then propagates).</summary>
    public static async ValueTask DeleteSelection(Diagram diagram)
    {
        var wasSuspended = diagram.SuspendRefresh;
        if (!wasSuspended) diagram.SuspendRefresh = true;

        try
        {
            foreach (var sm in diagram.GetSelectedModels().ToArray())
            {
                if (sm.Locked)
                    continue;

                if (sm is GroupModel group && (await diagram.Options.Constraints.ShouldDeleteGroup(group)))
                {
                    diagram.Groups.Delete(group);
                }
                else if (sm is NodeModel node && (await diagram.Options.Constraints.ShouldDeleteNode(node)))
                {
                    diagram.Nodes.Remove(node);
                }
                else if (sm is BaseLinkModel link && (await diagram.Options.Constraints.ShouldDeleteLink(link)))
                {
                    diagram.Links.Remove(link);
                }
            }
        }
        finally
        {
            if (!wasSuspended)
            {
                diagram.SuspendRefresh = false;
                diagram.Refresh();
            }
        }
    }

    /// <summary>Toggles grouping of the selected nodes: when any of them is in a group, those groups are removed; otherwise two or more ungrouped selected nodes are grouped. Does nothing when <see cref="Options.DiagramOptions.Groups"/> is disabled.</summary>
    public static ValueTask Grouping(Diagram diagram)
    {
        if (!diagram.Options.Groups.Enabled)
            return ValueTask.CompletedTask;

        if (!diagram.GetSelectedModels().Any())
            return ValueTask.CompletedTask;

        var selectedNodes = diagram.Nodes.Where(n => n.Selected).ToArray();
        var nodesWithGroup = selectedNodes.Where(n => n.Group != null).ToArray();
        if (nodesWithGroup.Length > 0)
        {
            // Ungroup
            foreach (var group in nodesWithGroup.GroupBy(n => n.Group!).Select(g => g.Key))
            {
                diagram.Groups.Remove(group);
            }
        }
        else
        {
            // Group
            if (selectedNodes.Length < 2)
                return ValueTask.CompletedTask;

            if (selectedNodes.Any(n => n.Group != null))
                return ValueTask.CompletedTask;

            diagram.Groups.Group(selectedNodes);
        }

        return ValueTask.CompletedTask;
    }
}
