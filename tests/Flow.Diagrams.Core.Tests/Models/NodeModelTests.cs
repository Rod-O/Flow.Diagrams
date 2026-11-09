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
}
