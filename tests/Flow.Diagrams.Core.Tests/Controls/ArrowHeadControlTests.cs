using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.PathGenerators;
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

    [Fact]
    public async Task DraggingTheSourceEnd_ShouldMoveTheSource_WithoutReversingTheLink()
    {
        // Arrange
        var (diagram, link, oldSource, target, newSource) = ThreePorts();
        var sourceAttached = 0;
        var targetAttached = 0;
        link.SourceAttached += _ => sourceAttached++;
        link.TargetAttached += _ => targetAttached++;

        // Act
        await new ArrowHeadControl(source: true).OnPointerDown(diagram, link, Pointer());
        diagram.TriggerPointerUp(newSource, Pointer());

        // Assert
        ((SinglePortAnchor)link.Source).Port.Should().BeSameAs(newSource);
        ((SinglePortAnchor)link.Target).Port.Should().BeSameAs(target);
        oldSource.Links.Should().NotContain(link);
        sourceAttached.Should().Be(1);
        targetAttached.Should().Be(0);
    }

    [Fact]
    public async Task DraggingTheSourceEnd_ShouldKeepTheTargetAttached_WhileDragging()
    {
        // Arrange
        var (diagram, link, _, target, _) = ThreePorts();

        // Act
        await new ArrowHeadControl(source: true).OnPointerDown(diagram, link, Pointer());

        // Assert
        link.Source.Should().BeOfType<PositionAnchor>();
        ((SinglePortAnchor)link.Target).Port.Should().BeSameAs(target);
    }

    [Fact]
    public void GetAngle_ShouldBeTheDirectionOfEachLinksEnd_WhenOneControlIsSharedByTwoLinks()
    {
        // Arrange
        var (diagram, right, _, _, _) = ThreePorts();
        var a = right.Source.Model!;
        var down = diagram.Links.Add(new LinkModel(new SinglePortAnchor((PortModel)a),
            new SinglePortAnchor(AddPort(diagram, new Point(100, 400)))));
        var control = new ArrowHeadControl(source: false);

        // Act
        control.GetPosition(down);
        var rightAngle = control.GetAngle(right);
        var downAngle = control.GetAngle(down);

        // Assert
        rightAngle.Should().BeApproximately(0, 1);
        downAngle.Should().BeApproximately(90, 1);
    }

    private static (TestDiagram, LinkModel, PortModel, PortModel, PortModel) ThreePorts()
    {
        var diagram = new TestDiagram();
        diagram.SetContainer(new Rectangle(0, 0, 1000, 1000));
        diagram.Options.Links.DefaultPathGenerator = new StraightPathGenerator();
        var source = AddPort(diagram, new Point(100, 100));
        var target = AddPort(diagram, new Point(400, 100));
        var newSource = AddPort(diagram, new Point(100, 300));
        var link = diagram.Links.Add(new LinkModel(new SinglePortAnchor(source), new SinglePortAnchor(target)));
        return (diagram, link, source, target, newSource);
    }

    private static PortModel AddPort(TestDiagram diagram, Point position)
    {
        var node = diagram.Nodes.Add(new NodeModel(position) { Size = new Size(10, 10) });
        return node.AddPort(new PortModel(node) { Initialized = true, Position = position, Size = new Size(10, 10) });
    }

    private static PointerEventArgs Pointer() =>
        new(100, 100, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true);
}
