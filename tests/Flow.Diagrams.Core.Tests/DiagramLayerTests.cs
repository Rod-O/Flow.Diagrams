using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests;

public class DiagramLayerTests
{
    [Fact]
    public void OrderedSelectables_ShouldDrawAHigherLayerAboveALowerOneWhateverTheOrder()
    {
        // Arrange
        var diagram = new TestDiagram();
        var top = diagram.Nodes.Add(new NodeModel { Layer = 1 });
        var bottom = diagram.Nodes.Add(new NodeModel());

        // Act
        var drawn = diagram.OrderedSelectables;

        // Assert
        bottom.Order.Should().BeGreaterThan(top.Order);
        drawn.Should().ContainInOrder(bottom, top);
    }

    [Fact]
    public void ChangingLayer_ShouldResort()
    {
        // Arrange
        var diagram = new TestDiagram();
        var a = diagram.Nodes.Add(new NodeModel());
        var b = diagram.Nodes.Add(new NodeModel());

        // Act
        a.Layer = 1;

        // Assert
        diagram.OrderedSelectables.Should().ContainInOrder(b, a);
    }

    [Fact]
    public void SendToFront_ShouldStayBelowAHigherLayer()
    {
        // Arrange
        var diagram = new TestDiagram();
        var label = diagram.Nodes.Add(new NodeModel { Layer = 1 });
        var a = diagram.Nodes.Add(new NodeModel());
        var b = diagram.Nodes.Add(new NodeModel());

        // Act
        diagram.SendToFront(a);

        // Assert
        diagram.OrderedSelectables.Should().ContainInOrder(b, a, label);
    }

    [Fact]
    public void SendToBack_ShouldStayAboveALowerLayer()
    {
        // Arrange
        var diagram = new TestDiagram();
        var a = diagram.Nodes.Add(new NodeModel { Layer = 1 });
        var b = diagram.Nodes.Add(new NodeModel { Layer = 1 });
        var background = diagram.Nodes.Add(new NodeModel());

        // Act
        diagram.SendToBack(b);

        // Assert
        diagram.OrderedSelectables.Should().ContainInOrder(background, b, a);
    }
}
