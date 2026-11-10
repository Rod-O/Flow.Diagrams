using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Routers;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.ChangeTracking;

public class ChangeTrackerTests
{
    [Fact]
    public void Refresh_ShouldRaiseChangedWithAppearanceAndGeometry()
    {
        // Arrange
        var node = new NodeModel();
        var raised = new List<ModelChanges>();
        node.Changed += (_, changes) => raised.Add(changes);

        // Act
        node.Refresh();

        // Assert
        raised.Should().Equal(ModelChanges.Appearance | ModelChanges.Geometry);
    }

    [Fact]
    public void SelectedChange_ShouldRaiseAppearanceOnly()
    {
        // Arrange
        var node = new NodeModel();
        var raised = new List<ModelChanges>();
        node.Changed += (_, changes) => raised.Add(changes);

        // Act
        node.Selected = true;

        // Assert
        raised.Should().Equal(ModelChanges.Appearance);
    }

    private const ModelChanges AppearanceAndGeometry = ModelChanges.Appearance | ModelChanges.Geometry;

    [Fact]
    public void Batch_ShouldRaiseChangedOncePerModel_WithTheUnionOfItsChanges()
    {
        // Arrange
        var (diagram, node, _, _) = CreateLinkedNodes();
        var raised = Record(node);

        // Act
        diagram.Batch(() =>
        {
            node.Title = "a";
            node.Selected = true;
            node.Refresh();
        });

        // Assert
        raised.Should().Equal(AppearanceAndGeometry);
    }

    [Fact]
    public void GeometryChangeOfANode_ShouldRouteEachAttachedLinkOncePerFlush()
    {
        // Arrange
        var (diagram, node, _, link) = CreateLinkedNodes();
        var router = (CountingRouter)link.Router!;
        router.Calls = 0;
        var raised = Record(link);

        // Act
        diagram.Batch(() =>
        {
            node.SetPosition(10, 10);
            node.SetPosition(20, 20);
            node.Refresh();
        });

        // Assert
        router.Calls.Should().Be(1);
        raised.Should().Equal(AppearanceAndGeometry);
    }

    [Fact]
    public void GeometryChangeOfALink_ShouldRedrawItsLabels()
    {
        // Arrange
        var (_, _, _, link) = CreateLinkedNodes();
        var label = link.AddLabel("x");
        var raised = Record(label);

        // Act
        link.Refresh();

        // Assert
        raised.Should().Equal(ModelChanges.Appearance);
    }

    [Fact]
    public void ChildGeometryChange_ShouldUpdateTheGroupOnce()
    {
        // Arrange
        var (diagram, child, sibling, _) = CreateLinkedNodes();
        var group = diagram.Groups.Group(child, sibling);
        var raised = Record(group);

        // Act
        diagram.Batch(() => child.SetPosition(500, 400));

        // Assert
        raised.Should().Equal(AppearanceAndGeometry);
        var bounds = group.GetBounds()!;
        var childBounds = child.GetBounds()!;
        bounds.Left.Should().BeLessThanOrEqualTo(childBounds.Left);
        bounds.Top.Should().BeLessThanOrEqualTo(childBounds.Top);
        bounds.Right.Should().BeGreaterThanOrEqualTo(childBounds.Right);
        bounds.Bottom.Should().BeGreaterThanOrEqualTo(childBounds.Bottom);
    }

    [Fact]
    public void AGroupThatMovesWhileRefitting_ShouldBeRaisedAndItsPortLinksRoutedOncePerFlush()
    {
        // Arrange
        var (diagram, child, sibling, _) = CreateLinkedNodes();
        var group = diagram.Groups.Group(child, sibling);
        var outside = diagram.Nodes.Add(new NodeModel(new Point(900, 0)) { Size = new Size(100, 50) });
        var router = new CountingRouter();
        var link = diagram.Links.Add(new LinkModel(group.AddPort(PortAlignment.Right), outside.AddPort(PortAlignment.Left))
        {
            Router = router
        });
        router.Calls = 0;
        var raisedGroup = Record(group);
        var raisedLink = Record(link);
        var groupPositionBefore = group.Position;

        // Act
        diagram.Batch(() => child.UpdatePositionSilently(-200, -100));

        // Assert
        group.Position.Should().NotBe(groupPositionBefore);
        raisedGroup.Should().Equal(AppearanceAndGeometry);
        raisedLink.Should().Equal(AppearanceAndGeometry);
        router.Calls.Should().Be(1);
    }

    [Fact]
    public void MarksMadeByAChangedHandler_ShouldFlushInTheSameFlush_AsAFurtherPass()
    {
        // Arrange
        var (diagram, _, nodeB, link) = CreateLinkedNodes();
        var order = new List<string>();
        var marked = false;
        link.Changed += (_, _) =>
        {
            order.Add("link");
            if (marked)
                return;

            marked = true;
            nodeB.MarkChanged(ModelChanges.Appearance);
        };
        nodeB.Changed += (_, _) => order.Add("nodeB");

        // Act
        diagram.Batch(() => link.Refresh());
        var orderWhenBatchReturned = order.ToList();

        // Assert
        orderWhenBatchReturned.Should().Equal("link", "nodeB");
    }

    [Fact]
    public void AHandlerThatAlwaysMarks_ShouldThrowAfterMaxPasses()
    {
        // Arrange
        var (diagram, node, _, _) = CreateLinkedNodes();
        void Remark(Model model, ModelChanges _) => model.MarkChanged(ModelChanges.Appearance);
        node.Changed += Remark;

        // Act
        var act = () => diagram.Batch(() => node.Refresh());

        // Assert
        act.Should().Throw<DiagramsException>()
            .WithMessage("Model changes did not settle after 64 passes; a Changed handler keeps marking models.");
        node.Changed -= Remark;
        var raised = Record(node);
        node.MarkChanged(ModelChanges.Appearance);
        raised.Should().Equal(ModelChanges.Appearance);
        diagram.SuspendRefresh.Should().BeFalse();
    }

    [Fact]
    public void AModelRemovedWhileMarked_ShouldNotBeRaisedOrRouted()
    {
        // Arrange
        var (diagram, node, _, link) = CreateLinkedNodes();
        var router = (CountingRouter)link.Router!;
        router.Calls = 0;
        var raised = Record(link);

        // Act
        diagram.Batch(() =>
        {
            node.SetPosition(10, 10);
            diagram.Links.Remove(link);
        });

        // Assert
        raised.Should().BeEmpty();
        router.Calls.Should().Be(0);
    }

    [Fact]
    public void AHandlerThatThrows_ShouldNotStopTheRestOfThePass()
    {
        // Arrange
        var (diagram, first, second, _) = CreateLinkedNodes();
        first.Changed += (_, _) => throw new InvalidOperationException("boom");
        var raisedSecond = Record(second);

        // Act
        var act = () => diagram.Batch(() =>
        {
            first.MarkChanged(ModelChanges.Appearance);
            second.MarkChanged(ModelChanges.Appearance);
        });

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("boom");
        raisedSecond.Should().Equal(ModelChanges.Appearance);
        second.MarkChanged(ModelChanges.Appearance);
        raisedSecond.Should().Equal(ModelChanges.Appearance, ModelChanges.Appearance);
    }

    [Fact]
    public void NestedBatches_ShouldFlushOnceAtTheOuterEnd()
    {
        // Arrange
        var (diagram, node, _, _) = CreateLinkedNodes();
        var raised = Record(node);
        var raisedAfterInnerBatch = -1;

        // Act
        diagram.Batch(() =>
        {
            node.Refresh();
            diagram.Batch(() => node.Refresh());
            raisedAfterInnerBatch = raised.Count;
        });

        // Assert
        raisedAfterInnerBatch.Should().Be(0);
        raised.Should().Equal(AppearanceAndGeometry);
    }

    [Fact]
    public void AModelOutsideADiagram_ShouldRaiseAtOnce()
    {
        // Arrange
        var node = new NodeModel();
        var raised = Record(node);

        // Act
        node.Selected = true;

        // Assert
        raised.Should().Equal(ModelChanges.Appearance);
    }

    [Fact]
    public void PointerMoveThatDragsTwoNodes_ShouldRaiseEachNodeAndLinkOnce()
    {
        // Arrange
        var (diagram, a, b, link) = CreateLinkedNodes();
        a.Selected = true;
        b.Selected = true;
        var router = (CountingRouter)link.Router!;
        diagram.TriggerPointerDown(a, Pointer(10, 10));
        var raisedA = Record(a);
        var raisedB = Record(b);
        var raisedLink = Record(link);
        router.Calls = 0;

        // Act
        diagram.TriggerPointerMove(null, Pointer(40, 30));

        // Assert
        raisedA.Should().HaveCount(1);
        raisedB.Should().HaveCount(1);
        raisedLink.Should().HaveCount(1);
        router.Calls.Should().Be(1);
    }

    [Fact]
    public void PointerMoveWithNoGesture_ShouldNotRaiseDiagramChanged()
    {
        // Arrange
        var (diagram, _, _, _) = CreateLinkedNodes();
        var raised = 0;
        diagram.Changed += () => raised++;

        // Act
        diagram.TriggerPointerMove(null, Pointer(40, 30));

        // Assert
        raised.Should().Be(0);
    }

    [Fact]
    public void PointerMoveThatSnapsANewLinkToAPort_ShouldRouteTheLinkOnce()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.SetContainer(new Rectangle(0, 0, 1000, 400));
        diagram.Options.Links.EnableSnapping = true;
        diagram.Options.Links.SnappingRadius = 60;
        var router = new CountingRouter();
        diagram.Options.Links.Factory = (_, source, target) =>
            new LinkModel(new Flow.Diagrams.Core.Anchors.SinglePortAnchor((PortModel)source), target) { Router = router };
        var node1 = diagram.Nodes.Add(new NodeModel(new Point(100, 50)));
        var node2 = diagram.Nodes.Add(new NodeModel(new Point(160, 50)));
        var port1 = node1.AddPort(new PortModel(node1) { Initialized = true, Position = new Point(110, 60), Size = new Size(10, 20) });
        node2.AddPort(new PortModel(node2) { Initialized = true, Position = new Point(170, 60), Size = new Size(10, 20) });
        diagram.TriggerPointerDown(port1, Pointer(100, 100));
        router.Calls = 0;

        // Act
        diagram.TriggerPointerMove(null, Pointer(140, 100));

        // Assert
        router.Calls.Should().Be(1);
    }

    private static Flow.Diagrams.Core.Events.PointerEventArgs Pointer(double x, double y) =>
        new(x, y, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, string.Empty, true);

    private static (TestDiagram Diagram, NodeModel A, NodeModel B, LinkModel Link) CreateLinkedNodes()
    {
        var diagram = new TestDiagram();
        var a = diagram.Nodes.Add(new NodeModel(new Point(0, 0)) { Size = new Size(100, 50) });
        var b = diagram.Nodes.Add(new NodeModel(new Point(300, 0)) { Size = new Size(100, 50) });
        var link = diagram.Links.Add(new LinkModel(a.AddPort(PortAlignment.Right), b.AddPort(PortAlignment.Left))
        {
            Router = new CountingRouter()
        });
        return (diagram, a, b, link);
    }

    private static List<ModelChanges> Record(Model model)
    {
        var raised = new List<ModelChanges>();
        model.Changed += (_, changes) => raised.Add(changes);
        return raised;
    }

    private sealed class CountingRouter : Router
    {
        public int Calls { get; set; }

        public override Point[] GetRoute(Diagram diagram, BaseLinkModel link)
        {
            Calls++;
            return [];
        }
    }
}
