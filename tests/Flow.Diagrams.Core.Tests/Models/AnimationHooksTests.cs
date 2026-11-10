using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Models;

public class AnimationHooksTests
{
    [Fact]
    public void CssClass_WhenChanged_ShouldRaiseChanged()
    {
        // Arrange
        var node = new NodeModel();
        var raised = 0;
        node.Changed += (_, _) => raised++;

        // Act
        node.CssClass = "diagram-pulse";

        // Assert
        node.CssClass.Should().Be("diagram-pulse");
        raised.Should().Be(1);
    }

    [Fact]
    public void CssClass_WhenSetToTheSameValue_ShouldNotRaiseChanged()
    {
        // Arrange
        var node = new NodeModel { CssClass = "a" };
        var raised = 0;
        node.Changed += (_, _) => raised++;

        // Act
        node.CssClass = "a";

        // Assert
        raised.Should().Be(0);
    }

    [Fact]
    public void CssClass_OnALink_ShouldRaiseChangedWithoutRoutingAgain()
    {
        // Arrange
        var link = new LinkModel(new NodeModel(), new NodeModel());
        Model? sender = null;
        link.Changed += (m, _) => sender = m;

        // Act
        link.CssClass = "highlight";

        // Assert
        sender.Should().BeSameAs(link);
    }

    [Fact]
    public void Flow_WhenChanged_ShouldRaiseChanged()
    {
        // Arrange
        var link = new LinkModel(new NodeModel(), new NodeModel());
        var raised = 0;
        link.Changed += (_, _) => raised++;

        // Act
        link.Flow = new LinkFlow();
        link.Flow = new LinkFlow();
        link.Flow = null;

        // Assert
        raised.Should().Be(2);
    }

    [Fact]
    public void LinkFlow_Defaults_ShouldMarchForwardAtFortyUnitsPerSecond()
    {
        // Act
        var flow = new LinkFlow();

        // Assert
        flow.Should().Be(new LinkFlow(Speed: 40, Dash: 8, Gap: 6, Reverse: false));
    }

    [Theory]
    [InlineData(-1, 8, 6)]
    [InlineData(40, 0, 6)]
    [InlineData(40, 8, -1)]
    [InlineData(double.NaN, 8, 6)]
    public void LinkFlow_InvalidValues_ShouldThrow(double speed, double dash, double gap)
    {
        // Act
        var create = () => new LinkFlow(speed, dash, gap);
        var with = () => new LinkFlow() with { Speed = speed, Dash = dash, Gap = gap };

        // Assert
        create.Should().Throw<ArgumentOutOfRangeException>();
        with.Should().Throw<ArgumentOutOfRangeException>();
    }
}
