using Flow.Diagrams.Algorithms;
using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Algorithms;

public class LinksReconnectionAlgorithmsTests
{
    [Fact]
    public void ReconnectLinksToClosestPorts_ShouldMeasureBetweenPortCentres()
    {
        // Arrange: by top-left corners the big port is closer, by centres the small one is
        var diagram = new TestDiagram();
        var source = diagram.Nodes.Add(new NodeModel());
        var target = diagram.Nodes.Add(new NodeModel());
        var sourcePort = AddPort(source, new Point(0, 0), new Size(10, 10));
        var bigPort = AddPort(target, new Point(20, 0), new Size(100, 100));
        var smallPort = AddPort(target, new Point(30, 0), new Size(2, 2));
        var link = diagram.Links.Add(new LinkModel(sourcePort, bigPort));

        // Act
        diagram.ReconnectLinksToClosestPorts();

        // Assert
        TargetPort(link).Should().BeSameAs(smallPort);
    }

    [Fact]
    public void ReconnectLinksToClosestPorts_ShouldSkipPorts_ThatCanAttachToRefuses()
    {
        // Arrange
        var diagram = new TestDiagram();
        var source = diagram.Nodes.Add(new NodeModel());
        var target = diagram.Nodes.Add(new NodeModel());
        var refused = AddPort(target, new Point(20, 0), new Size(10, 10));
        var allowed = AddPort(target, new Point(200, 0), new Size(10, 10));
        var sourcePort = source.AddPort(new RefusingPort(source, refused) { Position = new Point(0, 0), Size = new Size(10, 10) });
        var link = diagram.Links.Add(new LinkModel(sourcePort, allowed));

        // Act
        diagram.ReconnectLinksToClosestPorts();

        // Assert
        TargetPort(link).Should().BeSameAs(allowed);
    }

    [Fact]
    public void ReconnectLinksToClosestPorts_ShouldNotMoveALinkOntoALockedPort()
    {
        // Arrange
        var diagram = new TestDiagram();
        var source = diagram.Nodes.Add(new NodeModel());
        var target = diagram.Nodes.Add(new NodeModel());
        var near = AddPort(source, new Point(100, 0), new Size(10, 10));
        near.Locked = true;
        var far = AddPort(source, new Point(0, 0), new Size(10, 10));
        var targetPort = AddPort(target, new Point(200, 0), new Size(10, 10));
        var link = diagram.Links.Add(new LinkModel(far, targetPort));

        // Act
        diagram.ReconnectLinksToClosestPorts();

        // Assert
        ((SinglePortAnchor)link.Source).Port.Should().BeSameAs(far);
    }

    [Fact]
    public void ReconnectLinksToClosestPorts_ShouldNotMoveALockedLink()
    {
        // Arrange
        var diagram = new TestDiagram();
        var source = diagram.Nodes.Add(new NodeModel());
        var target = diagram.Nodes.Add(new NodeModel());
        var sourcePort = AddPort(source, new Point(0, 0), new Size(10, 10));
        var near = AddPort(target, new Point(20, 0), new Size(10, 10));
        var far = AddPort(target, new Point(200, 0), new Size(10, 10));
        var link = diagram.Links.Add(new LinkModel(sourcePort, far) { Locked = true });

        // Act
        diagram.ReconnectLinksToClosestPorts();

        // Assert
        TargetPort(link).Should().BeSameAs(far);
    }

    [Fact]
    public void ReconnectLinksToClosestPorts_ShouldNotMoveALink_WhoseEndIsOnALockedNode()
    {
        // Arrange
        var diagram = new TestDiagram();
        var source = diagram.Nodes.Add(new NodeModel { Locked = true });
        var target = diagram.Nodes.Add(new NodeModel());
        var far = AddPort(source, new Point(0, 0), new Size(10, 10));
        var near = AddPort(source, new Point(100, 0), new Size(10, 10));
        var targetPort = AddPort(target, new Point(200, 0), new Size(10, 10));
        var link = diagram.Links.Add(new LinkModel(far, targetPort));

        // Act
        diagram.ReconnectLinksToClosestPorts();

        // Assert
        ((SinglePortAnchor)link.Source).Port.Should().BeSameAs(far);
    }

    private static PortModel AddPort(NodeModel node, Point position, Size size)
        => node.AddPort(new PortModel(node) { Position = position, Size = size });

    private static PortModel TargetPort(BaseLinkModel link) => ((SinglePortAnchor)link.Target).Port;

    private class RefusingPort : PortModel
    {
        private readonly PortModel _refused;

        public RefusingPort(NodeModel parent, PortModel refused) : base(parent) => _refused = refused;

        public override bool CanAttachTo(ILinkable other) => other != _refused && base.CanAttachTo(other);
    }
}
