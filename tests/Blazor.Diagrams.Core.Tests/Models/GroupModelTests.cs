using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Blazor.Diagrams.Core.Tests.Models;

public class GroupModelTests
{
    private static NodeModel Node(double x, double y, double width, double height) =>
        new(new Point(x, y)) { Size = new Size(width, height) };

    [Fact]
    public void Constructor_UniformPadding_ShouldWrapChildrenWithTheSamePaddingOnEverySide()
    {
        // Arrange
        var children = new[] { Node(100, 100, 50, 20), Node(200, 150, 30, 30) };

        // Act
        var group = new GroupModel(children, padding: 10);

        // Assert
        group.Position.Should().Be(new Point(90, 90));
        group.Size.Should().Be(new Size(150, 100));
        group.Padding.Should().Be(10);
        group.PaddingBySide.Should().Be(new GroupPadding(10));
    }

    [Fact]
    public void Constructor_PaddingBySide_ShouldApplyEachSide()
    {
        // Arrange
        var children = new[] { Node(100, 100, 50, 20) };

        // Act
        var group = new GroupModel(children, new GroupPadding(Left: 2, Top: 5, Right: 3, Bottom: 1.5));

        // Assert
        group.Position.Should().Be(new Point(98, 95));
        group.Size.Should().Be(new Size(55, 26.5));
    }

    [Fact]
    public void Padding_WithPaddingBySide_ShouldBeTheSmallestSideRounded()
    {
        var group = new GroupModel(new[] { Node(0, 0, 10, 10) }, new GroupPadding(4, 12.6, 4, 2.4));

        group.Padding.Should().Be(2);
    }

    [Fact]
    public void MovingAChild_ShouldReapplyPaddingBySide()
    {
        // Arrange
        var child = Node(100, 100, 50, 20);
        var group = new GroupModel(new[] { child }, new GroupPadding(2, 5, 2, 2));

        // Act
        child.SetPosition(120, 140);

        // Assert
        group.Position.Should().Be(new Point(118, 135));
        group.Size.Should().Be(new Size(54, 27));
    }

    [Fact]
    public void UpdateDimensions_ShouldUseOverriddenChildrenBounds()
    {
        // Arrange: a child drawn around its origin, measured by its own extent instead of Position + Size.
        var child = new NodeModel(new Point(100, 100));

        // Act
        var group = new CentredChildrenGroup(new[] { child }, extent: 4);

        // Assert
        group.Position.Should().Be(new Point(95, 95));
        group.Size.Should().Be(new Size(10, 10));
    }

    [Fact]
    public void UpdateDimensions_ShouldWaitWhileChildrenCannotBeMeasured()
    {
        var group = new GroupModel(new[] { new NodeModel(new Point(100, 100)) }, padding: 10);

        group.Size.Should().Be(Size.Zero);
    }

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

    private sealed class CentredChildrenGroup : GroupModel
    {
        private readonly double _extent;

        public CentredChildrenGroup(NodeModel[] children, double extent) : base(new NodeModel[0], new GroupPadding(1))
        {
            _extent = extent;
            foreach (var child in children)
                AddChild(child);
        }

        protected override Rectangle? GetChildrenBounds()
        {
            var first = Children[0].Position;
            return new Rectangle(first.X - _extent, first.Y - _extent, first.X + _extent, first.Y + _extent);
        }
    }
}
