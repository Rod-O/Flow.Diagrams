using Bunit;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class LinkRendererTests
{
    [Fact]
    public void ShouldDrawHalosBeforeTheLink()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        diagram.Options.Links.HaloColor = "#fafafa";
        var link = new LinkModel(new NodeModel(), new NodeModel());
        link.SetHalos(new[] { new LinkHalo(new Point(250, 27), new Point(250, 33.5), 6) });

        // Act
        var cut = ctx.RenderComponent<LinkRenderer>(parameters => parameters
            .Add(n => n.Link, link)
            .AddCascadingValue(diagram));

        // Assert
        var halo = cut.Find("g.diagram-link > path.diagram-link-halo");
        halo.GetAttribute("d").Should().Be("M 250 27 L 250 33.5");
        halo.GetAttribute("stroke").Should().Be("#fafafa");
        halo.GetAttribute("stroke-width").Should().Be("6");
        halo.GetAttribute("pointer-events").Should().Be("none");
    }

    [Fact]
    public void ALinkTypeThatIsNotALinkModelAndHasNoWidget_ShouldFailNamingTheMissingRegistration()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var link = new BareLink(new NodeModel(), new NodeModel());

        // Act
        var render = () => ctx.RenderComponent<LinkRenderer>(parameters => parameters
            .Add(n => n.Link, link)
            .AddCascadingValue(diagram));

        // Assert
        render.Should().Throw<DiagramsException>()
            .WithMessage($"*{nameof(BareLink)}*{nameof(BlazorDiagram.RegisterComponent)}*");
    }

    private class BareLink : BaseLinkModel
    {
        public BareLink(NodeModel source, NodeModel target) : base(new ShapeIntersectionAnchor(source), new ShapeIntersectionAnchor(target))
        {
        }
    }
}
