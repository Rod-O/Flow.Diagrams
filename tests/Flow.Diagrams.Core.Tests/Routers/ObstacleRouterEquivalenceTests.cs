using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.PathGenerators;
using Flow.Diagrams.Core.Routers;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Routers;

/// <summary>
/// <see cref="ObstacleRouter"/> must produce, point for point, the routes of <see cref="ReferenceObstacleRouter"/>,
/// the original implementation, on the same diagram.
/// </summary>
public class ObstacleRouterEquivalenceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RouteAll_ShouldMatchTheOriginalRouter(int seed)
    {
        // Arrange: the same random diagram twice, one routed by each router.
        var options = new ObstacleRouterOptions();
        var (optimised, optimisedLinks) = Build(seed, new ObstacleRouter(options));
        var (reference, referenceLinks) = Build(seed, new ReferenceObstacleRouter(options));

        // Act
        ((ObstacleRouter)optimisedLinks[0].Router!).RouteAll(optimised);
        ((ReferenceObstacleRouter)referenceLinks[0].Router!).RouteAll(reference);

        // Assert
        optimisedLinks.Select(l => l.Route).Should().BeEquivalentTo(referenceLinks.Select(l => l.Route), o => o.WithStrictOrdering());
        optimisedLinks.Sum(l => l.Route!.Length).Should().BeGreaterThan(optimisedLinks.Count, "the routes should bend around things");
    }

    [Fact]
    public void GetRoute_AfterAMove_ShouldMatchTheOriginalRouter()
    {
        // Arrange
        var (optimised, optimisedLinks) = Build(7, new ObstacleRouter());
        var (reference, referenceLinks) = Build(7, new ReferenceObstacleRouter());

        // Act: moving a node refreshes its links against the routes the others have.
        foreach (var diagram in new[] { optimised, reference })
            foreach (var node in diagram.Nodes.Take(5))
                node.SetPosition(node.Position.X + 37, node.Position.Y - 23);

        // Assert
        optimisedLinks.Select(l => l.Route).Should().BeEquivalentTo(referenceLinks.Select(l => l.Route), o => o.WithStrictOrdering());
    }

    /// <summary>
    /// Random nodes, some overlapping, with a port on each side; links between random ports, some sharing one (a net),
    /// some with markers, and a few drawn by another router with diagonal vertices, which the routed links must price
    /// as placed segments too.
    /// </summary>
    private static (TestDiagram, List<LinkModel>) Build(int seed, Router router)
    {
        var random = new Random(seed);
        var diagram = new TestDiagram();
        var nodes = new List<NodeModel>();
        for (var i = 0; i < 12; i++)
        {
            var node = diagram.Nodes.Add(new NodeModel(new Point(random.Next(0, 6) * 90 + random.Next(0, 30), random.Next(0, 5) * 90 + random.Next(0, 30)))
            {
                Size = new Size(random.Next(30, 80), random.Next(30, 70)),
            });
            foreach (var alignment in new[] { PortAlignment.Top, PortAlignment.Right, PortAlignment.Bottom, PortAlignment.Left })
                AddPort(node, alignment);
            nodes.Add(node);
        }

        PortModel AnyPort() => nodes[random.Next(nodes.Count)].Ports[random.Next(4)];
        var links = new List<LinkModel>();
        for (var i = 0; i < 20; i++)
        {
            var link = new LinkModel(new SinglePortAnchor(AnyPort()), new SinglePortAnchor(AnyPort()));
            if (random.Next(8) == 0)
            {
                link.PathGenerator = new StraightPathGenerator();
                link.Vertices.Add(new LinkVertexModel(link, new Point(random.Next(0, 900), random.Next(0, 700))));
            }
            else
            {
                link.Router = router;
                if (random.Next(3) == 0)
                    link.TargetMarker = LinkMarker.Arrow;
                if (random.Next(5) == 0)
                    link.SourceMarker = LinkMarker.Arrow;
            }
            diagram.Links.Add(link);
            if (link.Router == router)
                links.Add(link);
        }

        return (diagram, links);
    }

    private static void AddPort(NodeModel node, PortAlignment alignment)
    {
        var b = node.GetBounds()!;
        var centre = alignment switch
        {
            PortAlignment.Left => new Point(b.Left, b.Top + b.Height / 2),
            PortAlignment.Right => new Point(b.Right, b.Top + b.Height / 2),
            PortAlignment.Top => new Point(b.Left + b.Width / 2, b.Top),
            _ => new Point(b.Left + b.Width / 2, b.Bottom),
        };
        var port = node.AddPort(new PortModel(node, alignment, new Point(centre.X - 5, centre.Y - 5), new Size(10, 10)));
        port.Initialized = true;
    }
}
