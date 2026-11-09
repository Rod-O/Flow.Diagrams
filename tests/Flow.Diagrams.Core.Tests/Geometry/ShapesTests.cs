using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Geometry;

public class ShapesTests
{
    [Fact]
    public void Circle_of_a_wide_node_is_inscribed_in_the_shorter_side_and_centred()
    {
        var node = new NodeModel(new Point(100, 50)) { Size = new Size(40, 20) };

        var circle = (Ellipse)Shapes.Circle(node);

        (circle.Cx, circle.Cy, circle.Rx, circle.Ry).Should().Be((120, 60, 10, 10));
    }

    [Fact]
    public void Circle_of_a_tall_node_is_inscribed_in_the_shorter_side_and_centred()
    {
        var node = new NodeModel(new Point(0, 0)) { Size = new Size(20, 40) };

        var circle = (Ellipse)Shapes.Circle(node);

        (circle.Cx, circle.Cy, circle.Rx, circle.Ry).Should().Be((10, 20, 10, 10));
    }

    [Fact]
    public void Shapes_of_an_unmeasured_node_do_not_throw()
    {
        var node = new NodeModel(new Point(5, 5));

        Shapes.Rectangle(node).GetIntersectionsWithLine(new Line(new Point(0, 0), new Point(10, 10))).Should().NotBeNull();
        Shapes.Circle(node).GetIntersectionsWithLine(new Line(new Point(0, 0), new Point(10, 10))).Should().NotBeNull();
        Shapes.Ellipse(node).GetIntersectionsWithLine(new Line(new Point(0, 0), new Point(10, 10))).Should().NotBeNull();
    }
}
