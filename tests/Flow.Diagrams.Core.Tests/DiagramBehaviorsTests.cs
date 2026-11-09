using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Flow.Diagrams.Core.Tests;

public class DiagramBehaviorsTests
{
    [Fact]
    public void GetBehavior_ShouldReturnARegisteredSubclass_WhenTheExactTypeIsNotRegistered()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.UnregisterBehavior<DragNewLinkBehavior>();
        var custom = new CustomDragNewLinkBehavior(diagram);
        diagram.RegisterBehavior(custom);

        // Act
        var found = diagram.GetBehavior<DragNewLinkBehavior>();

        // Assert
        found.Should().BeSameAs(custom);
    }

    [Fact]
    public void GetBehavior_ShouldPreferTheExactType_OverASubclass()
    {
        // Arrange
        var diagram = new TestDiagram();
        var exact = diagram.GetBehavior<DragNewLinkBehavior>();
        diagram.RegisterBehavior(new CustomDragNewLinkBehavior(diagram));

        // Act
        var found = diagram.GetBehavior<DragNewLinkBehavior>();

        // Assert
        found.Should().BeSameAs(exact);
    }

    [Fact]
    public async Task DragNewLinkControl_ShouldStartALink_WhenASubclassOfDragNewLinkBehaviorIsRegistered()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.SetContainer(new Rectangle(0, 0, 1000, 400));
        diagram.UnregisterBehavior<DragNewLinkBehavior>();
        diagram.RegisterBehavior(new CustomDragNewLinkBehavior(diagram));
        var node = diagram.Nodes.Add(new NodeModel(new Point(100, 50)));

        // Act
        await new DragNewLinkControl(1, 0.5).OnPointerDown(diagram, node, Pointer());

        // Assert
        diagram.Links.Single().Source.Model.Should().BeSameAs(node);
    }

    private static PointerEventArgs Pointer() =>
        new(100, 100, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true);

    private class CustomDragNewLinkBehavior : DragNewLinkBehavior
    {
        public CustomDragNewLinkBehavior(Diagram diagram) : base(diagram)
        {
        }
    }
}
