using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Models;

public class NodeModelTests
{
    [Fact]
    public void CanAttachTo_ShouldRefuseALockedNode()
    {
        var source = new NodeModel();
        var target = new NodeModel { Locked = true };

        source.CanAttachTo(target).Should().BeFalse();
    }

    [Fact]
    public void CanAttachTo_ShouldAcceptAnUnlockedNode()
    {
        new NodeModel().CanAttachTo(new NodeModel()).Should().BeTrue();
    }

    [Fact]
    public void SetSize_ShouldStoreTheSizeRefreshTheNodeAndItsLinksAndRemeasureItsPorts()
    {
        // Arrange
        var diagram = new TestDiagram();
        var node = diagram.Nodes.Add(new NodeModel { ControlledSize = true, Size = new Size(100, 60) });
        var port = node.AddPort(PortAlignment.Right);
        port.Initialized = true;
        var link = diagram.Links.Add(new LinkModel(node, diagram.Nodes.Add(new NodeModel())));
        var nodeRefreshes = 0;
        var linkRefreshes = 0;
        node.Changed += _ => nodeRefreshes++;
        link.Changed += _ => linkRefreshes++;

        // Act
        node.SetSize(new Size(200, 80));

        // Assert
        node.Size.Should().Be(new Size(200, 80));
        nodeRefreshes.Should().Be(1);
        linkRefreshes.Should().Be(1);
        port.Initialized.Should().BeFalse();
    }

    [Fact]
    public void SetSize_WithTheCurrentSize_ShouldDoNothing()
    {
        // Arrange
        var node = new NodeModel { ControlledSize = true, Size = new Size(100, 60) };
        var port = node.AddPort(PortAlignment.Right);
        port.Initialized = true;
        var refreshes = 0;
        node.Changed += _ => refreshes++;

        // Act
        node.SetSize(new Size(100, 60));

        // Assert
        refreshes.Should().Be(0);
        port.Initialized.Should().BeTrue();
    }
}
