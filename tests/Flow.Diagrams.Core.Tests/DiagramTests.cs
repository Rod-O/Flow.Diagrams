using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using System;
using Xunit;

namespace Flow.Diagrams.Core.Tests;

public class DiagramTests
{
    [Fact]
    public void GetScreenPoint_ShouldReturnCorrectPoint()
    {
        // Arrange
        var diagram = new TestDiagram();

        // Act
        diagram.SetZoom(1.234);
        diagram.UpdatePan(50, 50);
        diagram.SetContainer(new Rectangle(30, 65, 1000, 793));
        var pt = diagram.GetScreenPoint(100, 200);

        // Assert
        pt.X.Should().Be(203.4); // 2*X + panX + left
        pt.Y.Should().Be(361.8); // 2*Y + panY + top
    }

    [Fact]
    public void ZoomToFit_ShouldUseSelectedNodesIfAny()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.Options.Zoom.Maximum = 10; // room for the fit this test computes
        diagram.SetContainer(new Rectangle(new Point(0, 0), new Size(1080, 768)));
        diagram.Nodes.Add(new NodeModel(new Point(50, 50))
        {
            Size = new Size(100, 80)
        });
        diagram.SelectModel(diagram.Nodes[0], true);

        // Act
        diagram.ZoomToFit(10);

        // Assert
        diagram.Zoom.Should().BeApproximately(7.68, 0.001);
        diagram.Pan.X.Should().Be(-307.2);
        diagram.Pan.Y.Should().Be(-307.2);
    }

    [Fact]
    public void ZoomToFit_ShouldUseNodesWhenNoneSelected()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.Options.Zoom.Maximum = 10; // room for the fit this test computes
        diagram.SetContainer(new Rectangle(new Point(0, 0), new Size(1080, 768)));
        diagram.Nodes.Add(new NodeModel(new Point(50, 50))
        {
            Size = new Size(100, 80)
        });

        // Act
        diagram.ZoomToFit(10);

        // Assert
        diagram.Zoom.Should().BeApproximately(7.68, 0.001);
        diagram.Pan.X.Should().Be(-307.2);
        diagram.Pan.Y.Should().Be(-307.2);
    }

    [Fact]
    public void ZoomToFit_ShouldTriggerAppropriateEvents()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.SetContainer(new Rectangle(new Point(0, 0), new Size(1080, 768)));
        diagram.Nodes.Add(new NodeModel(new Point(50, 50))
        {
            Size = new Size(100, 80)
        });

        var refreshes = 0;
        var zoomChanges = 0;
        var panChanges = 0;

        // Act
        diagram.Changed += () => refreshes++;
        diagram.ZoomChanged += () => zoomChanges++;
        diagram.PanChanged += () => panChanges++;
        diagram.ZoomToFit(10);

        // Assert
        refreshes.Should().Be(1);
        zoomChanges.Should().Be(1);
        panChanges.Should().Be(1);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.1)]
    public void Zoom_ShoulClampToMinimumValue(double zoomValue)
    {
        var diagram = new TestDiagram();
        diagram.SetZoom(zoomValue);
        Assert.Equal(diagram.Zoom, diagram.Options.Zoom.Minimum);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(-0.00001)]
    public void Zoom_ThrowExceptionWhenLessThan0(double zoomValue)
    {
        var diagram = new TestDiagram();
        Assert.Throws<ArgumentException>(() => diagram.SetZoom(zoomValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(-0.00001)]
    public void ZoomOptions_ThrowExceptionWhenLessThan0(double zoomValue)
    {
        var diagram = new TestDiagram();
        Assert.Throws<ArgumentException>(() => diagram.Options.Zoom.Minimum = zoomValue);
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(0.01, 0.1)]
    [InlineData(1.5, 1.5)]
    public void SetZoom_ShouldStayWithinTheZoomOptions(double requested, double expected)
    {
        var diagram = new TestDiagram();
        diagram.Options.Zoom.Minimum = 0.1;
        diagram.Options.Zoom.Maximum = 2;

        diagram.SetZoom(requested);

        diagram.Zoom.Should().Be(expected);
    }

    [Fact]
    public void Batch_WhenActionThrows_RestoresRefreshAndRefreshesOnce()
    {
        // Arrange
        var diagram = new TestDiagram();
        var refreshes = 0;
        diagram.Changed += () => refreshes++;

        // Act
        var batch = () => diagram.Batch(() => throw new InvalidOperationException());

        // Assert
        batch.Should().Throw<InvalidOperationException>();
        diagram.SuspendRefresh.Should().BeFalse();
        refreshes.Should().Be(1);
    }

    [Fact]
    public void SelectModel_WithUnselectOthers_UnselectsOthersEvenWhenModelIsAlreadySelected()
    {
        // Arrange
        var diagram = new TestDiagram();
        var first = diagram.Nodes.Add(new NodeModel());
        var second = diagram.Nodes.Add(new NodeModel());
        diagram.SelectModel(first, false);
        diagram.SelectModel(second, false);

        // Act
        diagram.SelectModel(first, true);

        // Assert
        first.Selected.Should().BeTrue();
        second.Selected.Should().BeFalse();
    }
}
