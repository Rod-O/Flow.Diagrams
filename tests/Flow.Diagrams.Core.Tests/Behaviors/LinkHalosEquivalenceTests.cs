using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.PathGenerators;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Behaviors;

/// <summary>
/// The behavior's halos, however it updates them, must equal what a full recompute of every link against every link
/// drawn below it gives: <see cref="ExpectedHalos"/>, the original quadratic algorithm kept here as the oracle.
/// </summary>
public class LinkHalosEquivalenceTests
{
    private const double Clearance = 2;

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    [InlineData(8, true)]
    public void EveryEdit_ShouldLeaveTheHalosOfAFullRecompute(int seed, bool registerAfterTheLinks)
    {
        // Arrange
        var random = new Random(seed);
        var diagram = new TestDiagram();
        diagram.Options.Links.DefaultPathGenerator = new StraightPathGenerator();
        if (!registerAfterTheLinks)
            diagram.RegisterBehavior(new LinkHalosBehavior(diagram));
        var nodes = Enumerable.Range(0, 30).Select(_ => AddNode(diagram, random)).ToList();
        var links = Enumerable.Range(0, 60).Select(_ => AddLink(diagram, nodes, random)).ToList();
        if (registerAfterTheLinks)
            diagram.RegisterBehavior(new LinkHalosBehavior(diagram));
        ExpectedHalos(diagram).Values.Sum(h => h.Count).Should().BeGreaterThan(0, "the diagram should have crossings to compare");
        AssertMatchesFullRecompute(diagram);

        // Act + Assert: after every kind of edit the behavior reacts to.
        for (var step = 0; step < 80; step++)
        {
            switch (random.Next(12))
            {
                case 0:
                case 1:
                case 2:
                    var node = nodes[random.Next(nodes.Count)];
                    node.SetPosition(node.Position.X + random.Next(-150, 151), node.Position.Y + random.Next(-150, 151));
                    break;
                case 3:
                    diagram.SendToFront(links[random.Next(links.Count)]);
                    break;
                case 4:
                    diagram.SendToBack(links[random.Next(links.Count)]);
                    break;
                case 5:
                    links[random.Next(links.Count)].Layer = random.Next(-1, 2);
                    break;
                case 6:
                    var toggled = links[random.Next(links.Count)];
                    toggled.Visible = !toggled.Visible;
                    break;
                case 7:
                    // A path generator takes effect at the link's next refresh.
                    var switched = links[random.Next(links.Count)];
                    switched.PathGenerator = random.Next(3) switch
                    {
                        0 => null,
                        1 => new SmoothPathGenerator(),
                        _ => new StraightPathGenerator(),
                    };
                    switched.Refresh();
                    break;
                case 8:
                    diagram.Options.Links.DefaultPathGenerator = diagram.Options.Links.DefaultPathGenerator is StraightPathGenerator
                        ? new SmoothPathGenerator()
                        : new StraightPathGenerator();
                    diagram.Batch(() => links.ForEach(l => l.Refresh()));
                    break;
                case 9:
                    // A tie in layer and order is drawn in the diagram's list order, which re-sorting may change.
                    links[random.Next(links.Count)].Order = links[random.Next(links.Count)].Order;
                    break;
                case 10:
                    nodes[random.Next(nodes.Count)].Order = random.Next(1, 200);
                    break;
                default:
                    var removed = links[random.Next(links.Count)];
                    diagram.Links.Remove(removed);
                    links.Remove(removed);
                    links.Add(AddLink(diagram, nodes, random));
                    break;
            }

            AssertMatchesFullRecompute(diagram);
        }
    }

    private static void AssertMatchesFullRecompute(TestDiagram diagram)
    {
        var expected = ExpectedHalos(diagram);
        foreach (var (link, halos) in expected)
            link.Halos.Should().Equal(halos);
    }

    /// <summary>The original full recompute: every visible link against every visible link drawn below it.</summary>
    private static Dictionary<BaseLinkModel, List<LinkHalo>> ExpectedHalos(Diagram diagram)
    {
        var drawn = diagram.OrderedSelectables.OfType<BaseLinkModel>().Where(l => l.Visible)
            .OrderBy(l => l.Layer).ThenBy(l => l.Order).ToList();
        var polylines = drawn
            .Select(l => (l.PathGenerator ?? diagram.Options.Links.DefaultPathGenerator) is StraightPathGenerator ? LinkGeometry.Polyline(l) : null)
            .ToList();
        var result = new Dictionary<BaseLinkModel, List<LinkHalo>>();
        for (var upper = 0; upper < drawn.Count; upper++)
        {
            var link = drawn[upper];
            var halos = new List<LinkHalo>();
            result[link] = halos;
            if (polylines[upper] is not { } over)
                continue;
            for (var lower = 0; lower < upper; lower++)
            {
                if (polylines[lower] is not { } under || LinkGeometry.SharePort(link, drawn[lower]))
                    continue;
                foreach (var (a, b) in LinkGeometry.Segments(over))
                foreach (var (c, d) in LinkGeometry.Segments(under))
                    if (LinkGeometry.Crossing(a, b, c, d) is { } at)
                        halos.Add(Halo(at, a, b, d - c, StrokeWidth(link), StrokeWidth(drawn[lower])));
            }
        }
        return result;
    }

    private static double StrokeWidth(BaseLinkModel link) => link is LinkModel l ? l.Width : 2;

    private static LinkHalo Halo(Point at, Point a, Point b, Point under, double overWidth, double underWidth)
    {
        var half = underWidth / 2 + Clearance;
        var width = overWidth + 2 * Clearance;
        if (LinkGeometry.Horizontal(a, b) && Math.Abs(under.X) < 1e-6)
            return new LinkHalo(new Point(at.X - half, at.Y), new Point(at.X + half, at.Y), width);
        if (Math.Abs(a.X - b.X) < 1e-6 && Math.Abs(under.Y) < 1e-6)
            return new LinkHalo(new Point(at.X, at.Y - half), new Point(at.X, at.Y + half), width);

        var along = (b - a).Normalize();
        var sine = Math.Max(Math.Abs(LinkGeometry.Cross(along, under.Normalize())), 0.25);
        var offset = along.Multiply(half / sine);
        return new LinkHalo(at - offset, at + offset, width);
    }

    private static NodeModel AddNode(TestDiagram diagram, Random random)
    {
        var node = diagram.Nodes.Add(new NodeModel(new Point(random.Next(0, 1200), random.Next(0, 900)))
        {
            Size = new Size(random.Next(40, 120), random.Next(30, 90)),
        });
        foreach (var alignment in new[] { PortAlignment.Top, PortAlignment.Right, PortAlignment.Bottom, PortAlignment.Left })
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
        return node;
    }

    /// <summary>A link between two random ports (often sharing one with another link), with a few vertices on some, wider on some.</summary>
    private static LinkModel AddLink(TestDiagram diagram, List<NodeModel> nodes, Random random)
    {
        PortModel AnyPort() => nodes[random.Next(nodes.Count)].Ports[random.Next(4)];
        var link = new LinkModel(new SinglePortAnchor(AnyPort()), new SinglePortAnchor(AnyPort()))
        {
            Width = random.Next(3) == 0 ? 4 : 2,
        };
        if (random.Next(3) == 0)
            for (var i = random.Next(1, 3); i > 0; i--)
                link.Vertices.Add(new LinkVertexModel(link, new Point(random.Next(0, 1300), random.Next(0, 1000))));
        if (random.Next(10) == 0)
            link.PathGenerator = new SmoothPathGenerator();
        return diagram.Links.Add(link);
    }
}
