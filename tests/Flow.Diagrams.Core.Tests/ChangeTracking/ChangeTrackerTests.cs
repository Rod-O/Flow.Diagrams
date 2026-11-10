using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
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
}
