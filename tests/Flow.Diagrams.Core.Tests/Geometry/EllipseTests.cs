using System.Linq;
using Flow.Diagrams.Core.Geometry;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Geometry;

public class EllipseTests
{
    [Fact]
    public void Segment_that_misses_the_ellipse_has_no_intersection()
    {
        var circle = new Ellipse(0, 0, 1, 1);

        circle.GetIntersectionsWithLine(new Line(new Point(-5, 3), new Point(5, 3))).Should().BeEmpty();
    }

    [Fact]
    public void Segment_that_crosses_the_ellipse_has_two_intersections()
    {
        var circle = new Ellipse(0, 0, 1, 1);

        var hits = circle.GetIntersectionsWithLine(new Line(new Point(-5, 0), new Point(5, 0))).ToList();

        hits.Should().Equal(new Point(-1, 0), new Point(1, 0));
    }

    [Fact]
    public void Tangent_segment_touches_the_ellipse_once()
    {
        var circle = new Ellipse(0, 0, 1, 1);

        circle.GetIntersectionsWithLine(new Line(new Point(-5, 1), new Point(5, 1))).Should().Equal(new Point(0, 1));
    }
}
