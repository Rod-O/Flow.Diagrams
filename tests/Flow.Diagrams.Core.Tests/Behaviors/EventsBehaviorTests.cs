using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Behaviors;

public class EventsBehaviorTests
{
    [Fact]
    public void Behavior_ShouldNotTriggerMouseClick_WhenItsRemoved()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.UnregisterBehavior<EventsBehavior>();
        var eventTriggered = false;

        // Act
        diagram.PointerClick += (m, e) => eventTriggered = true;
        diagram.TriggerPointerDown(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));
        diagram.TriggerPointerUp(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));

        // Assert
        eventTriggered.Should().BeFalse();
    }

    [Fact]
    public void Behavior_ShouldTriggerMouseClick_WhenMouseDownThenUpWithoutMove()
    {
        // Arrange
        var diagram = new TestDiagram();
        var eventTriggered = false;

        // Act
        diagram.PointerClick += (m, e) => eventTriggered = true;
        diagram.TriggerPointerDown(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));
        diagram.TriggerPointerUp(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));

        // Assert
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void Behavior_ShouldNotTriggerMouseClick_WhenMouseMoves()
    {
        // Arrange
        var diagram = new TestDiagram();
        var eventTriggered = false;

        // Act
        diagram.PointerClick += (m, e) => eventTriggered = true;
        diagram.TriggerPointerDown(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));
        diagram.TriggerPointerMove(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));
        diagram.TriggerPointerUp(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));

        // Assert
        eventTriggered.Should().BeFalse();
    }

    [Fact]
    public void Behavior_ShouldTriggerMouseDoubleClick_WhenTwoMouseClicksHappenWithinTime()
    {
        // Arrange
        var diagram = new TestDiagram();
        var eventTriggered = false;

        // Act
        diagram.PointerDoubleClick += (m, e) => eventTriggered = true;
        diagram.TriggerPointerClick(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));
        diagram.TriggerPointerClick(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));

        // Assert
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public async Task Behavior_ShouldNotTriggerMouseDoubleClick_WhenTimeExceeds500()
    {
        // Arrange
        var diagram = new TestDiagram();
        var eventTriggered = false;

        // Act
        diagram.PointerDoubleClick += (m, e) => eventTriggered = true;
        diagram.TriggerPointerClick(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));
        await Task.Delay(520);
        diagram.TriggerPointerClick(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));

        // Assert
        eventTriggered.Should().BeFalse();
    }

    [Fact]
    public void Behavior_ShouldTriggerMouseClick_OnlyWhenMouseDownWasAlsoTriggered_Issue204()
    {
        // Arrange
        var diagram = new TestDiagram();
        var eventTriggered = false;

        // Act
        diagram.PointerClick += (m, e) => eventTriggered = true;
        diagram.TriggerPointerUp(null, new PointerEventArgs(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true));

        // Assert
        eventTriggered.Should().BeFalse();
    }
    [Fact]
    public void Behavior_ShouldNotTriggerDoubleClick_WhenTheTwoClicksAreOnDifferentModels()
    {
        // Arrange
        var diagram = new TestDiagram();
        var node = new NodeModel();
        var eventTriggered = false;
        diagram.PointerDoubleClick += (m, e) => eventTriggered = true;

        // Act
        diagram.TriggerPointerClick(node, Click());
        diagram.TriggerPointerClick(null, Click());

        // Assert
        eventTriggered.Should().BeFalse();
    }

    [Fact]
    public void Behavior_ShouldNotTriggerDoubleClick_OnTheCanvas_AfterTheClickedNodeWasRemoved()
    {
        // Arrange
        var diagram = new TestDiagram();
        var node = diagram.Nodes.Add(new NodeModel());
        var eventTriggered = false;
        diagram.PointerDoubleClick += (m, e) => eventTriggered = true;

        // Act
        diagram.TriggerPointerClick(node, Click());
        diagram.Nodes.Remove(node);
        diagram.TriggerPointerClick(null, Click());

        // Assert
        eventTriggered.Should().BeFalse();
    }

    [Fact]
    public void Behavior_ShouldTriggerOneDoubleClick_WhenThreeClicksHappenWithinTime()
    {
        // Arrange
        var diagram = new TestDiagram();
        var node = new NodeModel();
        var doubleClicks = 0;
        diagram.PointerDoubleClick += (m, e) => doubleClicks++;

        // Act
        diagram.TriggerPointerClick(node, Click());
        diagram.TriggerPointerClick(node, Click());
        diagram.TriggerPointerClick(node, Click());

        // Assert
        doubleClicks.Should().Be(1);
    }

    private static PointerEventArgs Click() =>
        new(0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true);
}
