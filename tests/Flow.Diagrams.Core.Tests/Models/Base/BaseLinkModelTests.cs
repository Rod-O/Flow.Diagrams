using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.PathGenerators;
using Flow.Diagrams.Core.Routers;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Models.Base;

public class BaseLinkModelTests
{
    [Fact]
    public void SetSource_ShouldChangePropertiesAndTriggerEvent()
    {
        // Arrange
        var link = new LinkModel(sourcePort: new PortModel(null!), targetPort: null!);
        var parent = new NodeModel();
        var port = new PortModel(parent);
        var sp = new SinglePortAnchor(port);
        var eventsTriggered = 0;
        Anchor? oldSp = null;
        Anchor? newSp = null;
        BaseLinkModel? linkInstance = null;

        // Act
        link.SourceChanged += (l, o, n) =>
        {
            eventsTriggered++;
            linkInstance = l;
            oldSp = o;
            newSp = n;
        };

        link.SetSource(sp);

        // Assert
        eventsTriggered.Should().Be(1);
        link.Source.Should().BeSameAs(sp);
        oldSp.Should().NotBeNull();
        newSp.Should().BeSameAs(sp);
        linkInstance.Should().BeSameAs(link);
        link.Source.Model.Should().BeSameAs(port);
    }

    [Fact]
    public void SetTarget_WithNull_ThrowsAndKeepsTarget()
    {
        // Arrange
        var target = new PositionAnchor(Point.Zero);
        var link = new LinkModel(new SinglePortAnchor(new PortModel(new NodeModel())), target);

        // Act
        var setTarget = () => link.SetTarget(null!);

        // Assert
        setTarget.Should().Throw<ArgumentNullException>();
        link.Target.Should().BeSameAs(target);
    }

    [Fact]
    public void SetTarget_ShouldChangePropertiesAndTriggerEvent()
    {
        // Arrange
        var link = new LinkModel(new SinglePortAnchor(null!), new PositionAnchor(Point.Zero));
        var parent = new NodeModel();
        var port = new PortModel(parent);
        var tp = new SinglePortAnchor(port);
        var eventsTriggered = 0;
        Anchor? oldTp = null;
        Anchor? newTp = null;
        BaseLinkModel? linkInstance = null;

        // Act
        link.TargetChanged += (l, o, n) =>
        {
            eventsTriggered++;
            linkInstance = l;
            oldTp = o;
            newTp = n;
        };

        link.SetTarget(tp);

        // Assert
        eventsTriggered.Should().Be(1);
        link.Target.Should().BeSameAs(tp);
        oldTp.Should().BeOfType<PositionAnchor>();
        newTp.Should().BeSameAs(tp);
        linkInstance.Should().BeSameAs(link);
        link.Target!.Model.Should().BeSameAs(port);
    }

    [Fact]
    public void GetBounds_ShouldReturnPathBBox()
    {
        // Arrange
        var link = new LinkModel(new PositionAnchor(new Point(10, 5)), new PositionAnchor(new Point(100, 80)));
        link.Diagram = new TestDiagram();
        link.PathGenerator = new StraightPathGenerator();
        link.Router = new NormalRouter();

        // Act
        link.Refresh();
        var bounds = link.GetBounds()!;

        // Assert
        bounds.Left.Should().Be(10);
        bounds.Top.Should().Be(5);
        bounds.Width.Should().Be(90);
        bounds.Height.Should().Be(75);
    }

    [Fact]
    public void AddVertex_ShouldAppendTheVertexAndRaiseVertexAdded()
    {
        // Arrange
        var link = new LinkModel(new NodeModel(), new NodeModel());
        link.AddVertex(new Point(10, 10));
        BaseLinkModel? raisedLink = null;
        LinkVertexModel? raisedVertex = null;
        link.VertexAdded += (l, v) => (raisedLink, raisedVertex) = (l, v);

        // Act
        var vertex = link.AddVertex(new Point(20, 20));

        // Assert
        link.Vertices[1].Should().BeSameAs(vertex);
        raisedLink.Should().BeSameAs(link);
        raisedVertex.Should().BeSameAs(vertex);
    }

    [Fact]
    public void InsertVertex_ShouldInsertAtTheIndexAndRaiseVertexAdded()
    {
        // Arrange
        var link = new LinkModel(new NodeModel(), new NodeModel());
        link.AddVertex(new Point(10, 10));
        LinkVertexModel? raised = null;
        link.VertexAdded += (_, v) => raised = v;

        // Act
        var vertex = link.InsertVertex(0, new Point(5, 5));

        // Assert
        link.Vertices[0].Should().BeSameAs(vertex);
        vertex.Position.Should().Be(new Point(5, 5));
        raised.Should().BeSameAs(vertex);
    }
}
