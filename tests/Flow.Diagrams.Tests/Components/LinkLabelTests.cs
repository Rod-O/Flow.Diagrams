using Bunit;
using Flow.Diagrams.Components;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class LinkLabelTests
{
    [Fact]
    public void SvgLabels_ShouldDrawTextInAGroupInsteadOfAForeignObject()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        diagram.Options.Links.SvgLabels = true;
        var link = LabelledLink(diagram);

        // Act
        var cut = ctx.RenderComponent<LinkLabelRenderer>(parameters => parameters
            .Add(n => n.Label, link.Labels[0])
            .Add(n => n.Path, link.PathGeneratorResult!.FullPath)
            .AddCascadingValue(diagram));

        // Assert
        cut.FindAll("foreignObject").Should().BeEmpty();
        var label = cut.Find("g.diagram-link-label");
        label.GetAttribute("transform").Should().StartWith("translate(");
        label.QuerySelector("text")!.TextContent.Should().Be("4-20 mA");
    }

    [Fact]
    public void LabelsOnTop_ShouldDrawLabelsAfterEveryLinkAndNotInsideTheirLink()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<Rectangle>("FlowDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 800, 600));
        var diagram = new BlazorDiagram();
        diagram.Options.Links.LabelsOnTop = true;
        diagram.Options.Links.SvgLabels = true;
        var link = LabelledLink(diagram);

        // Act
        var cut = ctx.RenderComponent<DiagramCanvas>(p => p.Add(n => n.BlazorDiagram, diagram));

        // Assert
        cut.FindAll("g.diagram-link g.diagram-link-label").Should().BeEmpty();
        var svg = cut.Find("svg.diagram-svg-layer");
        var children = svg.Children.Select(c => c.GetAttribute("class") ?? "").ToList();
        children.IndexOf("diagram-link-labels-layer").Should().BeGreaterThan(children.FindLastIndex(c => c.StartsWith("diagram-link ")));
        cut.Find("g.diagram-link-labels").GetAttribute("data-link-id").Should().Be(link.Id);
    }

    private static LinkModel LabelledLink(BlazorDiagram diagram)
    {
        var a = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var b = diagram.Nodes.Add(new NodeModel(new Point(300, 0)) { Size = new Size(100, 60) });
        var link = new LinkModel(a, b);
        link.Labels.Add(new LinkLabelModel(link, "4-20 mA"));
        return diagram.Links.Add(link);
    }
}
