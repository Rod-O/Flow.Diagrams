using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;

namespace Catalog.Examples.Nodes;

/// <summary>Links attach where they cross a node's shape, so a round node must say it is round.</summary>
public class PortlessLinksRoundNode(Point position) : NodeModel(position)
{
    public override IShape GetShape() => Shapes.Circle(this);
}
