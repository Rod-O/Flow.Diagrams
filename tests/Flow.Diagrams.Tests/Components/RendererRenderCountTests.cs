using Bunit;
using Flow.Diagrams.Components;
using Flow.Diagrams.Components.Controls;
using Flow.Diagrams.Core.Controls;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class RendererRenderCountTests
{
    [Fact]
    public void AnUnrelatedCanvasRefreshAfterMount_ShouldNotRenderAnyElement()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<Rectangle>("FlowDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 800, 600));
        var diagram = new BlazorDiagram();
        var a = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var b = diagram.Nodes.Add(new NodeModel(new Point(300, 0)) { Size = new Size(100, 60) });
        var c = diagram.Nodes.Add(new NodeModel(new Point(0, 300)) { Size = new Size(100, 60) });
        var d = diagram.Nodes.Add(new NodeModel(new Point(300, 300)) { Size = new Size(100, 60) });
        var link = diagram.Links.Add(new LinkModel(a.AddPort(PortAlignment.Right), b.AddPort(PortAlignment.Left)));
        link.Vertices.Add(new LinkVertexModel(link, new Point(200, 100)));
        diagram.Groups.Group(c, d);
        var cut = ctx.RenderComponent<DiagramCanvas>(p => p.Add(n => n.BlazorDiagram, diagram));
        var before = RenderCounts(cut);

        // Act
        cut.InvokeAsync(diagram.Refresh);

        // Assert
                RenderCounts(cut).Should().Equal(before);
    }

    [Fact]
    public void NodeRenderer_OnResize_ShouldRaiseTheNodeOnceAndNotRaiseDiagramChanged()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var node = diagram.Nodes.Add(new NodeModel(new Point(0, 0)));
        var cut = ctx.RenderComponent<NodeRenderer>(p => p.Add(n => n.Node, node).AddCascadingValue(diagram));
        var nodeRaised = 0;
        var diagramRaised = 0;
        node.Changed += (_, _) => nodeRaised++;
        diagram.Changed += () => diagramRaised++;

        // Act
        cut.Instance.OnResize(new Size(120, 80));

        // Assert
        nodeRaised.Should().Be(1);
        diagramRaised.Should().Be(0);
    }

    [Fact]
    public void GroupRenderer_ShouldNotRenderAgain_WhenItsParentRendersWithTheSameGroup()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var group = diagram.Groups.Group(
            diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(50, 50) }),
            diagram.Nodes.Add(new NodeModel(new Point(100, 0)) { Size = new Size(50, 50) }));
        var cut = ctx.RenderComponent<GroupRenderer>(p => p.Add(n => n.Group, group).AddCascadingValue(diagram));

        // Act
        cut.Render();

        // Assert
        cut.RenderCount.Should().Be(1);
    }

    [Fact]
    public void PortRenderer_ShouldNotRenderAgain_WhenItsParentRendersWithTheSamePort()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var port = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(50, 50) }).AddPort(PortAlignment.Left);
        var cut = ctx.RenderComponent<PortRenderer>(p => p.Add(n => n.Port, port).AddCascadingValue(diagram));

        // Act
        cut.Render();

        // Assert
        cut.RenderCount.Should().Be(1);
    }

    [Fact]
    public void ControlsLayerRenderer_ShouldNotRenderAgain_AfterTwoChangesWhileARenderWasQueued()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var node = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(50, 50) });
        var controls = diagram.Controls.AddFor(node);
        // An event handler runs inside a render batch, so the second change finds the first render still queued.
        var cut = ctx.Render(builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(new object(), () =>
            {
                controls.Show();
                controls.Hide();
            }));
            builder.CloseElement();
            builder.OpenComponent<CascadingValue<BlazorDiagram>>(2);
            builder.AddAttribute(3, "Value", diagram);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(child =>
            {
                child.OpenComponent<ControlsLayerRenderer>(0);
                child.CloseComponent();
            }));
            builder.CloseComponent();
        });
        var layer = cut.FindComponent<ControlsLayerRenderer>();
        cut.Find("button").Click();
        var before = layer.RenderCount;

        // Act
        layer.Render();

        // Assert
        layer.RenderCount.Should().Be(before);
    }

    private static Dictionary<string, int> RenderCounts(IRenderedComponent<DiagramCanvas> cut) => new()
    {
        [nameof(NodeRenderer)] = Sum<NodeRenderer>(cut),
        [nameof(LinkRenderer)] = Sum<LinkRenderer>(cut),
        [nameof(GroupRenderer)] = Sum<GroupRenderer>(cut),
        [nameof(PortRenderer)] = Sum<PortRenderer>(cut),
        [nameof(LinkVertexRenderer)] = Sum<LinkVertexRenderer>(cut),
    };

    private static int Sum<T>(IRenderedComponent<DiagramCanvas> cut) where T : IComponent =>
        cut.FindComponents<T>().Should().NotBeEmpty().And.Subject.Sum(r => r.RenderCount);
}
