using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Routers;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Routers;

public class OrthogonalRouterTests
{
    [Theory]
    [InlineData(PortAlignment.TopRight, 90, 0, 100)]
    [InlineData(PortAlignment.BottomRight, 90, 90, 100)]
    [InlineData(PortAlignment.BottomLeft, 0, 90, 0)]
    [InlineData(PortAlignment.TopLeft, 0, 0, 0)]
    public void GetRoute_CornerPort_ShouldLeaveTheCornerHorizontally(PortAlignment alignment, double portX, double portY,
        double cornerX)
    {
        // Arrange
        var diagram = new TestDiagram();
        var source = new NodeModel(new Point(0, 0)) { Size = new Size(100, 100) };
        var target = new NodeModel(new Point(400, 300)) { Size = new Size(100, 100) };
        var sourcePort = source.AddPort(new PortModel(source, alignment, new Point(portX, portY), new Size(10, 10)));
        var targetPort = target.AddPort(new PortModel(target, PortAlignment.Left, new Point(400, 345), new Size(10, 10)));
        sourcePort.Initialized = targetPort.Initialized = true;
        diagram.Nodes.Add(new[] { source, target });
        var link = diagram.Links.Add(new LinkModel(new SinglePortAnchor(sourcePort), new SinglePortAnchor(targetPort)));

        // Act
        var route = new OrthogonalRouter().GetRoute(diagram, link);

        // Assert: the first leg runs level from where the link really ends, the point the anchor puts on the port.
        route.Should().NotBeEmpty();
        var end = link.Source.GetPosition(link, route)!;
        route[0].Y.Should().BeApproximately(end.Y, 1e-9);
        var exitsRight = alignment is PortAlignment.TopRight or PortAlignment.BottomRight;
        if (exitsRight)
            route[0].X.Should().BeGreaterThan(cornerX);
        else
            route[0].X.Should().BeLessThan(cornerX);
    }
}
