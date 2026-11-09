using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Controls;

public class ArrowHeadControlTests
{
    [Fact]
    public async Task OnPointerDown_ShouldThrowDiagramsException_WhenDragNewLinkBehaviorIsMissing()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.UnregisterBehavior<DragNewLinkBehavior>();
        var source = diagram.Nodes.Add(new NodeModel(new Point(0, 0)));
        var target = diagram.Nodes.Add(new NodeModel(new Point(200, 0)));
        var link = diagram.Links.Add(new LinkModel(source, target));

        // Act
        Func<Task> act = async () => await new ArrowHeadControl(source: false).OnPointerDown(diagram, link, Pointer());

        // Assert
        await act.Should().ThrowAsync<DiagramsException>().WithMessage("*DragNewLinkBehavior*");
    }

    private static PointerEventArgs Pointer() =>
        new(100, 100, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true);
}
