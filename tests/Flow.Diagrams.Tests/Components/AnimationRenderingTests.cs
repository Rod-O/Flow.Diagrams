using System.Xml.Linq;
using Bunit;
using Flow.Diagrams.Components;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Export;
using Flow.Diagrams.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class AnimationRenderingTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void NodeRenderer_CssClass_ShouldBeAddedToTheWrapperAndFollowChanges()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var node = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { CssClass = "diagram-pulse" });
        var cut = ctx.RenderComponent<NodeRenderer>(p => p.Add(n => n.Node, node).AddCascadingValue(diagram));
        cut.Find("div.diagram-node").ClassList.Should().Contain("diagram-pulse");

        // Act
        node.CssClass = "tripped";

        // Assert
        cut.WaitForAssertion(() =>
        {
            var classes = cut.Find("div.diagram-node").ClassList;
            classes.Should().Contain("tripped").And.NotContain("diagram-pulse");
        });
    }

    [Fact]
    public void GroupRenderer_CssClass_ShouldBeAddedToTheWrapper()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var a = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(50, 50) });
        var group = diagram.Groups.Group(a);

        // Act
        group.CssClass = "zone";
        var cut = ctx.RenderComponent<GroupRenderer>(p => p.Add(n => n.Group, group).AddCascadingValue(diagram));

        // Assert
        cut.Find("div.diagram-group").ClassList.Should().Contain("zone");
    }

    [Fact]
    public void LinkRenderer_CssClass_ShouldBeAddedToTheWrapperAndFollowChanges()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = TwoNodesAndALink(diagram);
        var cut = ctx.RenderComponent<LinkRenderer>(p => p.Add(n => n.Link, link).AddCascadingValue(diagram));
        cut.Find("g.diagram-link").ClassList.Should().NotContain("diagram-pulse");

        // Act
        link.CssClass = "diagram-pulse";

        // Assert
        cut.WaitForAssertion(() => cut.Find("g.diagram-link").ClassList.Should().Contain("diagram-pulse"));
    }

    [Fact]
    public void LinkWidget_WithFlow_ShouldDashTheStrokeAndSetTheAnimationProperties()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = TwoNodesAndALink(diagram);
        link.Flow = new LinkFlow(Speed: 28, Dash: 8, Gap: 6);

        // Act
        var cut = ctx.RenderComponent<LinkWidget>(p => p.Add(n => n.Link, link).AddCascadingValue(diagram));

        // Assert
        var stroke = cut.Find("path");
        stroke.GetAttribute("class").Should().Be("diagram-link-flow");
        stroke.GetAttribute("stroke-dasharray").Should().Be("8 6");
        stroke.GetAttribute("style").Should().Be("--diagram-flow-period:14px;--diagram-flow-duration:0.5s");
        cut.Find("path.selection-helper").HasAttribute("stroke-dasharray").Should().BeFalse();
    }

    [Fact]
    public void LinkWidget_WithReverseFlow_ShouldReverseTheAnimation()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = TwoNodesAndALink(diagram);
        link.Flow = new LinkFlow(Speed: 7, Dash: 4, Gap: 3, Reverse: true);

        // Act
        var cut = ctx.RenderComponent<LinkWidget>(p => p.Add(n => n.Link, link).AddCascadingValue(diagram));

        // Assert
        cut.Find("path").GetAttribute("style")
            .Should().Be("--diagram-flow-period:7px;--diagram-flow-duration:1s;animation-direction:reverse");
    }

    [Fact]
    public void LinkWidget_WithStillFlow_ShouldDashTheStrokeWithoutAnimating()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = TwoNodesAndALink(diagram);
        link.Flow = new LinkFlow(Speed: 0);

        // Act
        var cut = ctx.RenderComponent<LinkWidget>(p => p.Add(n => n.Link, link).AddCascadingValue(diagram));

        // Assert
        var stroke = cut.Find("path");
        stroke.GetAttribute("stroke-dasharray").Should().Be("8 6");
        stroke.HasAttribute("class").Should().BeFalse();
        stroke.HasAttribute("style").Should().BeFalse();
    }

    [Fact]
    public void LinkWidget_WithoutFlow_ShouldAddNothingToTheStroke()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = TwoNodesAndALink(diagram);

        // Act
        var cut = ctx.RenderComponent<LinkWidget>(p => p.Add(n => n.Link, link).AddCascadingValue(diagram));

        // Assert
        var stroke = cut.Find("path");
        stroke.Attributes.Select(a => a.Name).Should().BeEquivalentTo("d", "stroke-width", "fill", "stroke");
    }

    [Fact]
    public void LinkWidget_FlowTurnedOff_ShouldRedrawASolidStroke()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = TwoNodesAndALink(diagram);
        link.Flow = new LinkFlow();
        var cut = ctx.RenderComponent<LinkRenderer>(p => p.Add(n => n.Link, link).AddCascadingValue(diagram));

        // Act
        link.Flow = null;

        // Assert
        cut.WaitForAssertion(() => cut.FindAll("path[stroke-dasharray]").Should().BeEmpty());
    }

    [Fact]
    public async Task Export_ShouldDrawFlowingLinksSolid_AndKeepTheirCssClass()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var a = diagram.Nodes.Add(new SvgNodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var b = diagram.Nodes.Add(new SvgNodeModel(new Point(300, 0)) { Size = new Size(100, 60) });
        diagram.Links.Add(new LinkModel(a, b) { Flow = new LinkFlow(), CssClass = "water" });

        // Act
        var export = await DiagramSvgExporter.ExportAsync(diagram, ctx.Services);

        // Assert
        export.Svg.Should().NotContain("stroke-dasharray").And.NotContain("diagram-flow").And.NotContain("animation");
        XDocument.Parse(export.Svg).Descendants(Svg + "g").Select(g => (string?)g.Attribute("class"))
            .Should().Contain("diagram-link attached water");
    }

    private static LinkModel TwoNodesAndALink(BlazorDiagram diagram)
    {
        var a = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var b = diagram.Nodes.Add(new NodeModel(new Point(300, 0)) { Size = new Size(100, 60) });
        return diagram.Links.Add(new LinkModel(a, b));
    }
}
