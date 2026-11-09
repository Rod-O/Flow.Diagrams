using Bunit;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class GroupRendererTests
{
    [Fact]
    public void Children_ShouldBeDrawnInTheirLayerAndOrder()
    {
        // Arrange: the group lists a first, but b is in a higher layer and c is ordered before a.
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var (group, a, b, c) = GroupOfThree(diagram);
        b.Layer = 1;
        c.Order = a.Order - 1;

        // Act
        var cut = ctx.RenderComponent<GroupRenderer>(parameters => parameters
            .Add(n => n.Group, group)
            .AddCascadingValue(diagram));

        // Assert
        DrawnOrder(cut).Should().Equal(c.Id, a.Id, b.Id);
    }

    [Fact]
    public void SendingAChildToFront_ShouldRedrawTheGroupInTheNewOrder()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var diagram = new BlazorDiagram();
        var (group, a, b, c) = GroupOfThree(diagram);
        var cut = ctx.RenderComponent<GroupRenderer>(parameters => parameters
            .Add(n => n.Group, group)
            .AddCascadingValue(diagram));

        // Act
        diagram.SendToFront(a);

        // Assert
        cut.WaitForAssertion(() => DrawnOrder(cut).Should().Equal(b.Id, c.Id, a.Id));
    }

    private static (GroupModel, NodeModel, NodeModel, NodeModel) GroupOfThree(BlazorDiagram diagram)
    {
        var a = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(50, 50) });
        var b = diagram.Nodes.Add(new NodeModel(new Point(100, 0)) { Size = new Size(50, 50) });
        var c = diagram.Nodes.Add(new NodeModel(new Point(200, 0)) { Size = new Size(50, 50) });
        var group = diagram.Groups.Group(a, b, c);
        return (group, a, b, c);
    }

    private static List<string?> DrawnOrder(IRenderedComponent<GroupRenderer> cut) =>
        cut.FindAll(".children > [data-node-id]").Select(e => e.GetAttribute("data-node-id")).ToList();
}
