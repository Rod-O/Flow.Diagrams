using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Options;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Behaviors;

public class VirtualizationBehaviorTests
{
    private static TestDiagram CreateDiagram()
    {
        var diagram = new TestDiagram(new DiagramOptions { Virtualization = { Enabled = true } });
        diagram.SetContainer(new Rectangle(0, 0, 500, 500));
        return diagram;
    }

    private static NodeModel AddNode(Diagram diagram, double x, double y)
    {
        var node = new NodeModel(new Point(x, y)) { Size = new Size(50, 50) };
        return diagram.Nodes.Add(node);
    }

    [Fact]
    public void AddingANodeOutsideTheViewport_ShouldHideIt()
    {
        // Arrange
        var diagram = CreateDiagram();

        // Act
        var node = AddNode(diagram, 2000, 2000);

        // Assert
        node.Visible.Should().BeFalse();
    }

    [Fact]
    public void AddingANodeInsideTheViewport_ShouldKeepItVisible()
    {
        var diagram = CreateDiagram();

        var node = AddNode(diagram, 100, 100);

        node.Visible.Should().BeTrue();
    }

    [Fact]
    public void MovingANodeOutOfTheViewport_ShouldHideItWhenTheMoveEnds()
    {
        // Arrange
        var diagram = CreateDiagram();
        var node = AddNode(diagram, 100, 100);

        // Act
        node.Position = new Point(2000, 2000);
        node.TriggerMoved();

        // Assert
        node.Visible.Should().BeFalse();
    }

    [Fact]
    public void MovingAHiddenNodeIntoTheViewport_ShouldShowItWhenTheMoveEnds()
    {
        var diagram = CreateDiagram();
        var node = AddNode(diagram, 2000, 2000);

        node.Position = new Point(100, 100);
        node.TriggerMoved();

        node.Visible.Should().BeTrue();
    }

    [Fact]
    public void DisablingVirtualization_ShouldShowTheModelsItHidden_OnTheNextCheck()
    {
        // Arrange
        var diagram = CreateDiagram();
        var node = AddNode(diagram, 2000, 2000);
        node.Visible.Should().BeFalse();

        // Act
        diagram.Options.Virtualization.Enabled = false;
        diagram.SetPan(1, 1);

        // Assert
        node.Visible.Should().BeTrue();
    }

    [Fact]
    public void Virtualization_ShouldNotShowAModelTheUserHid()
    {
        // Arrange
        var diagram = CreateDiagram();
        var node = AddNode(diagram, 100, 100);
        node.Visible = false;

        // Act
        diagram.SetPan(1, 1);

        // Assert
        node.Visible.Should().BeFalse();
    }

    [Fact]
    public void Virtualization_ShouldNotShowAModelTheUserHid_AfterItWasDisabled()
    {
        var diagram = CreateDiagram();
        var node = AddNode(diagram, 100, 100);
        node.Visible = false;
        diagram.Options.Virtualization.Enabled = false;

        diagram.SetPan(1, 1);

        node.Visible.Should().BeFalse();
    }
}
