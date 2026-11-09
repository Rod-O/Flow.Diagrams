using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Groups;

public class SvgGroupsNode : SvgNodeModel
{
    public SvgGroupsNode(Point position, string label) : base(position)
    {
        Label = label;
        Size = new Size(120, 60);
        ControlledSize = true;
    }

    public string Label { get; }
}

public class SvgGroupsGroup(IEnumerable<NodeModel> children, string heading)
    : SvgGroupModel(children, new GroupPadding(Left: 30, Top: 48, Right: 30, Bottom: 30))
{
    public string Heading { get; } = heading;
}
