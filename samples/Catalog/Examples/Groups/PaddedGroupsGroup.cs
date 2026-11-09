using Flow.Diagrams.Core.Models;

namespace Catalog.Examples.Groups;

public class PaddedGroupsGroup(IEnumerable<NodeModel> children, string title, GroupPadding padding) : GroupModel(children, padding)
{
    public string Heading { get; } = title;
}
