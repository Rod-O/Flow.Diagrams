using Bunit;
using Flow.Diagrams.Components.Widgets;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class NavigatorWidgetTests
{
    [Fact]
    public void WithoutWidthAndHeight_ShouldRenderAFiniteViewBox()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        diagram.Nodes.Add(new NodeModel(new Point(10, 10)) { Size = new Size(50, 50) });
        var cut = ctx.RenderComponent<NavigatorWidget>(parameters => parameters.AddCascadingValue(diagram));

        // Act
        cut.InvokeAsync(() => diagram.SetContainer(new Rectangle(0, 0, 800, 600)));

        // Assert
        var viewBox = cut.Find("svg").GetAttribute("viewBox")!;
        viewBox.Should().NotContain("NaN").And.NotContain("Infinity");
        cut.Find("svg").GetAttribute("width").Should().NotBe("0");
    }

    [Fact]
    public void WithAZeroWidth_ShouldNotProduceANonFiniteViewBox()
    {
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        diagram.Nodes.Add(new NodeModel(new Point(10, 10)) { Size = new Size(50, 50) });
        var cut = ctx.RenderComponent<NavigatorWidget>(parameters => parameters
            .Add(n => n.Width, 0)
            .Add(n => n.Height, 0)
            .AddCascadingValue(diagram));

        cut.InvokeAsync(() => diagram.SetContainer(new Rectangle(0, 0, 800, 600)));

        var viewBox = cut.Find("svg").GetAttribute("viewBox")!;
        viewBox.Should().NotContain("NaN").And.NotContain("Infinity");
    }

    [Fact]
    public void Nodes_ShouldBeDrawnAsRectsWithTheirPosition()
    {
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        diagram.Nodes.Add(new NodeModel(new Point(10, 20)) { Size = new Size(50, 40) });
        var cut = ctx.RenderComponent<NavigatorWidget>(parameters => parameters
            .Add(n => n.Width, 200)
            .Add(n => n.Height, 100)
            .AddCascadingValue(diagram));

        cut.InvokeAsync(() => diagram.SetContainer(new Rectangle(0, 0, 800, 600)));

        var rect = cut.Find("rect.navigator-node");
        rect.GetAttribute("x").Should().Be("10");
        rect.GetAttribute("y").Should().Be("20");
        rect.GetAttribute("width").Should().Be("50");
        rect.GetAttribute("height").Should().Be("40");
    }
}
