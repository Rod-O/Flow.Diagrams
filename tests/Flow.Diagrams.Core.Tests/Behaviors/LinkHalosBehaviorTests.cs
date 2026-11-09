using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.PathGenerators;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Behaviors;

public class LinkHalosBehaviorTests
{
    [Fact]
    public void CrossingLinks_ShouldBreakOnlyTheLowerOne()
    {
        // Arrange
        var diagram = StraightDiagram();
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));

        // Act
        var (horizontal, vertical) = Cross(diagram);

        // Assert: the vertical link is drawn later, so it gets the halo, laid along itself across the horizontal one:
        // half the lower stroke (1) + clearance (2) each way, as wide as its own stroke (2) + clearance each side.
        horizontal.Halos.Should().BeEmpty();
        vertical.Halos.Should().ContainSingle().Which.Should().Be(new LinkHalo(new Point(250, 27), new Point(250, 33), 6));
    }

    [Fact]
    public void RegisteredAfterTheLinks_ShouldStillFindTheCrossings()
    {
        var diagram = StraightDiagram();
        var (_, vertical) = Cross(diagram);

        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));

        vertical.Halos.Should().ContainSingle();
    }

    [Fact]
    public void LinksSharingAPort_ShouldNotBreakEachOther()
    {
        // Arrange: a tee. The branch leaves the vertical link's source port and, through its vertices, runs across the
        // vertical link at y = -60, well above the horizontal link.
        var diagram = StraightDiagram();
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));
        var (_, vertical) = Cross(diagram);
        var source = ((SinglePortAnchor)vertical.Source).Port;
        var target = AddNode(diagram, 330, -90, 60, 60);
        var branch = new LinkModel(new SinglePortAnchor(source), new SinglePortAnchor(Port(target, PortAlignment.Left)));
        foreach (var (x, y) in new[] { (250.0, -120.0), (200.0, -120.0), (200.0, -60.0), (320.0, -60.0) })
            branch.Vertices.Add(new LinkVertexModel(branch, new Point(x, y)));

        // Act
        diagram.Links.Add(branch);

        // Assert
        branch.Halos.Should().BeEmpty();
    }

    [Fact]
    public void MovingTheLowerLinkAway_ShouldClearTheUpperLinksHalo()
    {
        // Arrange
        var diagram = StraightDiagram();
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));
        var (horizontal, vertical) = Cross(diagram);

        // Act: both ends of the horizontal link go down, below the vertical one.
        foreach (var node in new[] { ((SinglePortAnchor)horizontal.Source).Port.Parent, ((SinglePortAnchor)horizontal.Target).Port.Parent })
            node.SetPosition(node.Position.X, node.Position.Y + 500);

        // Assert
        vertical.Halos.Should().BeEmpty();
    }

    [Fact]
    public void RaisingTheLowerLinksLayer_ShouldMoveTheHaloToTheOtherLink()
    {
        // Arrange
        var diagram = StraightDiagram();
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));
        var (horizontal, vertical) = Cross(diagram);

        // Act
        horizontal.Layer = 1;

        // Assert
        vertical.Halos.Should().BeEmpty();
        horizontal.Halos.Should().ContainSingle();
    }

    [Fact]
    public void SmoothLinks_ShouldGetNoHalos()
    {
        var diagram = new TestDiagram();
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));

        var (_, vertical) = Cross(diagram);

        vertical.Halos.Should().BeEmpty();
    }

    private static TestDiagram StraightDiagram()
    {
        var diagram = new TestDiagram();
        diagram.Options.Links.DefaultPathGenerator = new StraightPathGenerator();
        return diagram;
    }

    /// <summary>A horizontal link along y = 30, then a vertical one along x = 250 that crosses it.</summary>
    private static (LinkModel Horizontal, LinkModel Vertical) Cross(TestDiagram diagram)
    {
        var a = AddNode(diagram, 0, 0, 100, 60);
        var b = AddNode(diagram, 400, 0, 100, 60);
        var c = AddNode(diagram, 200, -200, 100, 60);
        var d = AddNode(diagram, 200, 100, 100, 60);
        var horizontal = diagram.Links.Add(new LinkModel(new SinglePortAnchor(Port(a, PortAlignment.Right)), new SinglePortAnchor(Port(b, PortAlignment.Left))));
        var vertical = diagram.Links.Add(new LinkModel(new SinglePortAnchor(Port(c, PortAlignment.Bottom)), new SinglePortAnchor(Port(d, PortAlignment.Top))));
        return (horizontal, vertical);
    }

    private static NodeModel AddNode(TestDiagram diagram, double x, double y, double width, double height) =>
        diagram.Nodes.Add(new NodeModel(new Point(x, y)) { Size = new Size(width, height) });

    private static PortModel Port(NodeModel node, PortAlignment alignment)
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
        return port;
    }
}
