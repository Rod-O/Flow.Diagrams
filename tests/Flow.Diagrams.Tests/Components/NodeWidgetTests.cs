using Flow.Diagrams.Components;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;

using Bunit;

using FluentAssertions;

using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class NodeWidgetTests
{
    [Fact]
    public void DefaultNodeWidget_ShouldHaveSingleClassAndNoPorts_WhenItHasNoPortsAndNoSelectionNorGroup()
    {
        // Arrange
        using var ctx = new TestContext();
        var node = new NodeModel(Point.Zero);

        // Act
        var cut = ctx.RenderComponent<NodeWidget>(parameters => parameters
            .Add(n => n.Node, node));

        // Assert
        var content = cut.Find("div.default-node");
        content.ClassList.Should().ContainSingle();
        content.ClassList[0].Should().Be("default-node");
        content.TextContent.Trim().Should().Be("Title");

        var ports = cut.FindComponents<PortRenderer>();
        ports.Should().BeEmpty();
    }
}
