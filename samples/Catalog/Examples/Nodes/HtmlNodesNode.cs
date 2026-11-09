using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;

namespace Catalog.Examples.Nodes;

public class HtmlNodesNode(Point position, string name, string kind, string owner, string environment) : NodeModel(position)
{
    public string Name { get; set; } = name;
    public string Kind { get; } = kind;
    public string Owner { get; set; } = owner;
    public string Environment { get; set; } = environment;
}
