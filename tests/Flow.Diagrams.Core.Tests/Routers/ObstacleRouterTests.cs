using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Routers;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Routers;

public class ObstacleRouterTests
{
    private const double PortSize = 10;

    [Fact]
    public void GetRoute_FacingPortsWithNothingBetween_ShouldBeStraight()
    {
        // Arrange
        var (diagram, router) = Setup();
        var a = AddNode(diagram, 0, 0, 100, 60);
        var b = AddNode(diagram, 400, 0, 100, 60);
        var link = Connect(diagram, router, Port(a, PortAlignment.Right), Port(b, PortAlignment.Left));

        // Act
        var route = router.GetRoute(diagram, link);

        // Assert
        route.Should().BeEmpty();
    }

    [Fact]
    public void GetRoute_NodeBetweenFacingPorts_ShouldGoAroundIt()
    {
        // Arrange
        var (diagram, router) = Setup();
        var a = AddNode(diagram, 0, 0, 100, 60);
        var b = AddNode(diagram, 400, 0, 100, 60);
        var obstacle = AddNode(diagram, 200, -20, 100, 100);
        var link = Connect(diagram, router, Port(a, PortAlignment.Right), Port(b, PortAlignment.Left));

        // Act
        var polyline = Polyline(link, router.GetRoute(diagram, link));

        // Assert
        polyline.Count.Should().BeGreaterThan(2);
        Cuts(polyline, obstacle.GetBounds()!).Should().BeFalse();
        Orthogonal(polyline).Should().BeTrue();
    }

    [Fact]
    public void GetRoute_ExpensiveCrossings_ShouldGoAroundTheOtherNet()
    {
        var crossings = CrossingsWithAVerticalNet(crossCost: 100_000);

        crossings.Should().Be(0);
    }

    [Fact]
    public void GetRoute_CheapCrossings_ShouldCrossTheOtherNet()
    {
        var crossings = CrossingsWithAVerticalNet(crossCost: 1);

        crossings.Should().Be(1);
    }

    [Fact]
    public void GetRoute_TargetWithMarker_ShouldKeepAStraightLegForTheArrowhead()
    {
        // Arrange: the source sits just up and left of the target's left port, so the plain route would turn into the
        // port about 15 units before it, too short for the arrowhead.
        var diagram = new TestDiagram();
        var router = new ObstacleRouter(new ObstacleRouterOptions { MinStub = 40 });
        var a = AddNode(diagram, 230, 0, 100, 60);
        var b = AddNode(diagram, 300, 200, 100, 60);
        var link = Connect(diagram, router, Port(a, PortAlignment.Bottom), Port(b, PortAlignment.Left));
        link.TargetMarker = LinkMarker.Arrow;

        // Act
        var polyline = Polyline(link, router.GetRoute(diagram, link));

        // Assert
        var (last, end) = (polyline[^2], polyline[^1]);
        last.Y.Should().Be(end.Y);
        (end.X - last.X).Should().BeGreaterThanOrEqualTo(40);
        Orthogonal(polyline).Should().BeTrue();
    }

    [Fact]
    public void GetRoute_SourceWithMarker_ShouldKeepAStraightLegForTheArrowhead()
    {
        // Arrange: the target-marker case reversed; the arrowhead now sits at the source end.
        var diagram = new TestDiagram();
        var router = new ObstacleRouter(new ObstacleRouterOptions { MinStub = 40 });
        var a = AddNode(diagram, 230, 0, 100, 60);
        var b = AddNode(diagram, 300, 200, 100, 60);
        var link = Connect(diagram, router, Port(b, PortAlignment.Left), Port(a, PortAlignment.Bottom));
        link.SourceMarker = LinkMarker.Arrow;

        // Act
        var polyline = Polyline(link, router.GetRoute(diagram, link));

        // Assert
        var (start, first) = (polyline[0], polyline[1]);
        first.Y.Should().Be(start.Y);
        (start.X - first.X).Should().BeGreaterThanOrEqualTo(40);
        Orthogonal(polyline).Should().BeTrue();
    }

    [Fact]
    public void GetRoute_CornerPort_ShouldLeaveHorizontally()
    {
        // Arrange
        var (diagram, router) = Setup();
        var a = AddNode(diagram, 0, 0, 100, 60);
        var b = AddNode(diagram, 300, 200, 100, 60);
        var link = Connect(diagram, router, Port(a, PortAlignment.TopRight), Port(b, PortAlignment.Left));

        // Act
        var polyline = Polyline(link, router.GetRoute(diagram, link));

        // Assert
        polyline[1].Y.Should().Be(polyline[0].Y);
        polyline[1].X.Should().BeGreaterThan(polyline[0].X);
    }

    [Fact]
    public void GetRoute_TargetNotOnAPort_ShouldUseTheFallbackRouter()
    {
        // Arrange
        var (diagram, router) = Setup();
        var a = AddNode(diagram, 0, 0, 100, 60);
        var link = new LinkModel(new SinglePortAnchor(Port(a, PortAlignment.Right)), new PositionAnchor(new Point(300, 300)))
        {
            Router = router
        };
        diagram.Links.Add(link);

        // Act
        var route = router.GetRoute(diagram, link);

        // Assert
        route.Should().BeEmpty();
    }

    private static int CrossingsWithAVerticalNet(double crossCost)
    {
        // A vertical link from P down to Q walls off the middle; the horizontal link from A to B must either cross it
        // or go around one of its ends.
        var diagram = new TestDiagram();
        var router = new ObstacleRouter(new ObstacleRouterOptions { CrossCost = crossCost });
        var p = AddNode(diagram, 200, 0, 20, 20);
        var q = AddNode(diagram, 200, 400, 20, 20);
        var a = AddNode(diagram, 0, 180, 60, 60);
        var b = AddNode(diagram, 400, 220, 60, 60);
        var wall = Connect(diagram, router, Port(p, PortAlignment.Bottom), Port(q, PortAlignment.Top));
        var link = Connect(diagram, router, Port(a, PortAlignment.Right), Port(b, PortAlignment.Left));

        router.RouteAll(diagram);

        var wallLine = Polyline(wall, wall.Route!);
        var polyline = Polyline(link, link.Route!);
        Orthogonal(polyline).Should().BeTrue();
        return Segments(polyline).Sum(s => Segments(wallLine).Count(w => Crosses(s, w)));
    }

    private static (TestDiagram, ObstacleRouter) Setup() => (new TestDiagram(), new ObstacleRouter());

    private static NodeModel AddNode(TestDiagram diagram, double x, double y, double width, double height) =>
        diagram.Nodes.Add(new NodeModel(new Point(x, y)) { Size = new Size(width, height) });

    /// <summary>A measured port centred on the node edge the alignment names.</summary>
    private static PortModel Port(NodeModel node, PortAlignment alignment)
    {
        var b = node.GetBounds()!;
        var h = PortSize / 2;
        var centre = alignment switch
        {
            PortAlignment.Left => new Point(b.Left, b.Top + b.Height / 2),
            PortAlignment.Right => new Point(b.Right, b.Top + b.Height / 2),
            PortAlignment.Top => new Point(b.Left + b.Width / 2, b.Top),
            PortAlignment.Bottom => new Point(b.Left + b.Width / 2, b.Bottom),
            PortAlignment.TopRight => new Point(b.Right, b.Top),
            _ => throw new ArgumentOutOfRangeException(nameof(alignment)),
        };
        var port = node.AddPort(new PortModel(node, alignment, new Point(centre.X - h, centre.Y - h), new Size(PortSize, PortSize)));
        port.Initialized = true;
        return port;
    }

    private static LinkModel Connect(TestDiagram diagram, Router router, PortModel source, PortModel target) =>
        diagram.Links.Add(new LinkModel(new SinglePortAnchor(source), new SinglePortAnchor(target)) { Router = router });

    private static List<Point> Polyline(BaseLinkModel link, Point[] route) =>
        new[] { link.Source.GetPosition(link, route)! }.Concat(route).Append(link.Target.GetPosition(link, route)!).ToList();

    private static IEnumerable<(Point A, Point B)> Segments(IReadOnlyList<Point> polyline)
    {
        for (var i = 0; i + 1 < polyline.Count; i++)
            yield return (polyline[i], polyline[i + 1]);
    }

    private static bool Orthogonal(IReadOnlyList<Point> polyline) =>
        Segments(polyline).All(s => Math.Abs(s.A.X - s.B.X) < 1e-6 || Math.Abs(s.A.Y - s.B.Y) < 1e-6);

    private static bool Cuts(IReadOnlyList<Point> polyline, Rectangle r) => Segments(polyline).Any(s =>
        Math.Abs(s.A.Y - s.B.Y) < 1e-6
            ? s.A.Y > r.Top && s.A.Y < r.Bottom && Math.Max(s.A.X, s.B.X) > r.Left && Math.Min(s.A.X, s.B.X) < r.Right
            : s.A.X > r.Left && s.A.X < r.Right && Math.Max(s.A.Y, s.B.Y) > r.Top && Math.Min(s.A.Y, s.B.Y) < r.Bottom);

    private static bool Crosses((Point A, Point B) h, (Point A, Point B) v)
    {
        if (Math.Abs(h.A.Y - h.B.Y) >= 1e-6)
            (h, v) = (v, h);
        if (Math.Abs(h.A.Y - h.B.Y) >= 1e-6 || Math.Abs(v.A.X - v.B.X) >= 1e-6)
            return false;
        return v.A.X > Math.Min(h.A.X, h.B.X) && v.A.X < Math.Max(h.A.X, h.B.X)
               && h.A.Y > Math.Min(v.A.Y, v.B.Y) && h.A.Y < Math.Max(v.A.Y, v.B.Y);
    }
}
