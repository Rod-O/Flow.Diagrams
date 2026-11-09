using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Performance;

/// <summary>A round marker; links attach to its outline, so it needs no ports.</summary>
public class ManyLinksNode : SvgNodeModel
{
    public ManyLinksNode(Point position, int number) : base(position)
    {
        Number = number;
        Size = new Size(34, 34);
        ControlledSize = true;
    }

    public int Number { get; }

    public override IShape GetShape() => Shapes.Circle(this);
}
