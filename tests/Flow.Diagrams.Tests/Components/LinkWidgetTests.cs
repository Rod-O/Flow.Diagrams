using Bunit;
using Flow.Diagrams.Components;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Routers;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class LinkWidgetTests
{
    [Fact]
    public void RouterThatIgnoresVertices_ShouldDrawNoVertexHandles()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = LinkWithAVertex(diagram, new OrthogonalRouter());

        // Act
        var cut = ctx.RenderComponent<LinkWidget>(parameters => parameters
            .Add(n => n.Link, link)
            .AddCascadingValue(diagram));

        // Assert
        cut.FindAll("g.diagram-link-vertex").Should().BeEmpty();
    }

    [Fact]
    public void RouterThatFollowsVertices_ShouldDrawTheVertexHandles()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = LinkWithAVertex(diagram, new NormalRouter());

        // Act
        var cut = ctx.RenderComponent<LinkWidget>(parameters => parameters
            .Add(n => n.Link, link)
            .AddCascadingValue(diagram));

        // Assert
        cut.FindAll("g.diagram-link-vertex").Should().ContainSingle();
    }

    [Fact]
    public void SegmentableLinkWithARouterThatIgnoresVertices_ShouldNotCreateAVertexOnPointerDown()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        diagram.SetContainer(new Rectangle(0, 0, 1000, 1000));
        var link = LinkWithAVertex(diagram, new OrthogonalRouter());
        link.Segmentable = true;
        var cut = ctx.RenderComponent<LinkWidget>(parameters => parameters
            .Add(n => n.Link, link)
            .AddCascadingValue(diagram));

        // Act
        cut.Find("path.selection-helper").PointerDown();

        // Assert
        link.Vertices.Should().ContainSingle();
    }

    private static LinkModel LinkWithAVertex(BlazorDiagram diagram, Router router)
    {
        var a = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var b = diagram.Nodes.Add(new NodeModel(new Point(300, 0)) { Size = new Size(100, 60) });
        var link = new LinkModel(a, b) { Router = router };
        link.Vertices.Add(new LinkVertexModel(link, new Point(200, 100)));
        return diagram.Links.Add(link);
    }
}
