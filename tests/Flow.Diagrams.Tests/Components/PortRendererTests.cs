using Bunit;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class PortRendererTests
{
    [Fact]
    public void RenderedBeforeTheCanvasIsMeasured_ShouldMeasureOnceTheCanvasIs()
    {
        // Arrange: a node whose size the model controls never resizes, so nothing else would ever ask the port again.
        using var ctx = new TestContext();
        ctx.JSInterop.Setup<Rectangle>("FlowDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(110, 120, 120, 130));
        var diagram = new BlazorDiagram();
        var node = diagram.Nodes.Add(new NodeModel(new Point(100, 100)) { Size = new Size(50, 50), ControlledSize = true });
        var port = node.AddPort(PortAlignment.Left);

        var cut = ctx.RenderComponent<PortRenderer>(parameters => parameters
            .Add(n => n.Port, port)
            .AddCascadingValue(diagram));
        port.Initialized.Should().BeFalse();

        // Act
        cut.InvokeAsync(() => diagram.SetContainer(new Rectangle(10, 20, 810, 620)));

        // Assert
        cut.WaitForAssertion(() => port.Initialized.Should().BeTrue());
        port.Position.Should().Be(new Point(100, 100));
    }
}
