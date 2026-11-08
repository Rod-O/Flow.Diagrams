using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Blazor.Diagrams.Core.Tests.Models;

public class GroupModelTests
{
    [Fact]
    public void MovingTheOnlyChild_ShouldMoveTheGroupPortsWithTheGroup()
    {
        // Arrange: the group keeps its size, so the renderer will not re-measure its ports.
        var child = new NodeModel(new Point(100, 100)) { Size = new Size(50, 50) };
        var group = new GroupModel(new[] { child }, padding: 10);
        var port = group.AddPort(new PortModel(group, PortAlignment.Left, new Point(85, 110), new Size(10, 10)));

        // Act
        child.SetPosition(130, 140);

        // Assert
        group.Position.Should().Be(new Point(120, 130));
        group.Size.Should().Be(new Size(70, 70));
        port.Position.Should().Be(new Point(115, 150));
    }
}
