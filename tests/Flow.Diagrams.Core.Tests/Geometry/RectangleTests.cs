using System.Collections.Generic;
using System.Linq;
using Flow.Diagrams.Core.Geometry;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Geometry;

public class RectangleTests
{
    [Fact]
    public void Rectangles_with_the_same_edges_are_equal_as_objects()
    {
        object a = new Rectangle(1, 2, 3, 4);
        object b = new Rectangle(1, 2, 3, 4);

        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Rectangles_with_the_same_edges_find_each_other_as_dictionary_keys()
    {
        var map = new Dictionary<Rectangle, string> { [new Rectangle(1, 2, 3, 4)] = "found" };

        map[new Rectangle(1, 2, 3, 4)].Should().Be("found");
    }

    [Fact]
    public void Equality_operators_compare_edges_and_accept_null()
    {
        (new Rectangle(1, 2, 3, 4) == new Rectangle(1, 2, 3, 4)).Should().BeTrue();
        (new Rectangle(1, 2, 3, 4) != new Rectangle(1, 2, 3, 5)).Should().BeTrue();
        (new Rectangle(1, 2, 3, 4) == null).Should().BeFalse();
        ((Rectangle?)null == null).Should().BeTrue();
    }

    [Fact]
    public void Swapped_edges_are_normalized()
    {
        var r = new Rectangle(10, 20, 4, 6);

        (r.Left, r.Top, r.Right, r.Bottom).Should().Be((4, 6, 10, 20));
        (r.Width, r.Height).Should().Be((6, 14));
        r.Center.Should().Be(new Point(7, 13));
        r.Should().Be(new Rectangle(4, 6, 10, 20));
    }

    [Fact]
    public void Line_through_a_corner_yields_that_corner_once()
    {
        var r = new Rectangle(0, 0, 10, 10);

        var hits = r.GetIntersectionsWithLine(new Line(new Point(-5, -5), new Point(5, 5))).ToList();

        hits.Should().Equal(new Point(0, 0));
    }

    [Fact]
    public void Intersects_is_false_for_rectangles_that_only_share_an_edge()
    {
        new Rectangle(0, 0, 10, 10).Intersects(new Rectangle(10, 0, 20, 10)).Should().BeFalse();
        new Rectangle(0, 0, 10, 10).Intersects(new Rectangle(9, 9, 20, 20)).Should().BeTrue();
    }
}
