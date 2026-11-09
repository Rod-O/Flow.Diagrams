using Bunit;
using Flow.Diagrams.Components.Controls;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class RemoveControlWidgetTests
{
    [Fact]
    public void OnAnSvgGroup_ShouldDrawTheButtonWithoutAnHtmlWrapper()
    {
        // Arrange
        using var ctx = new TestContext();
        var group = new SvgGroupModel(Array.Empty<NodeModel>());

        // Act
        var cut = ctx.RenderComponent<RemoveControlWidget>(p => p
            .Add(w => w.Control, new RemoveControl(0, 0))
            .Add(w => w.Model, group));

        // Assert
        cut.FindAll("svg").Should().BeEmpty();
        cut.Find("circle").Should().NotBeNull();
    }

    [Fact]
    public void OnAnHtmlNode_ShouldWrapTheButtonInAnSvgElement()
    {
        using var ctx = new TestContext();

        var cut = ctx.RenderComponent<RemoveControlWidget>(p => p
            .Add(w => w.Control, new RemoveControl(0, 0))
            .Add(w => w.Model, new NodeModel(Point.Zero)));

        cut.Find("svg circle").Should().NotBeNull();
    }
}
