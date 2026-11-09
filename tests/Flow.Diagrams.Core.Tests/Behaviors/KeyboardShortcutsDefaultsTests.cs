using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Models;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Behaviors;

public class KeyboardShortcutsDefaultsTests
{
    [Fact]
    public async Task DeleteSelection_ShouldNotDeleteModel_WhenItsLocked()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.Nodes.Add(new NodeModel
        {
            Selected = true,
            Locked = true
        });

        // Act
        await KeyboardShortcutsDefaults.DeleteSelection(diagram);

        // Assert
        diagram.Nodes.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSelection_ShouldTakeIntoAccountGroupConstraint()
    {
        // Arrange
        var funcCalled = false;
        var diagram = new TestDiagram();
        diagram.Options.Constraints.ShouldDeleteGroup = _ =>
        {
            funcCalled = true;
            return ValueTask.FromResult(false);
        };
        diagram.Groups.Add(new GroupModel(Array.Empty<NodeModel>())
        {
            Selected = true
        });

        // Act
        await KeyboardShortcutsDefaults.DeleteSelection(diagram);

        // Assert
        funcCalled.Should().BeTrue();
        diagram.Groups.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSelection_ShouldTakeIntoAccountNodeConstraint()
    {
        // Arrange
        var funcCalled = false;
        var diagram = new TestDiagram();
        diagram.Options.Constraints.ShouldDeleteNode = _ =>
        {
            funcCalled = true;
            return ValueTask.FromResult(false);
        };
        diagram.Nodes.Add(new NodeModel
        {
            Selected = true
        });

        // Act
        await KeyboardShortcutsDefaults.DeleteSelection(diagram);

        // Assert
        funcCalled.Should().BeTrue();
        diagram.Nodes.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSelection_ShouldTakeIntoAccountLinkConstraint()
    {
        // Arrange
        var funcCalled = false;
        var diagram = new TestDiagram();
        diagram.Options.Constraints.ShouldDeleteLink = _ =>
        {
            funcCalled = true;
            return ValueTask.FromResult(false);
        };
        diagram.Nodes.Add(new NodeModel[]
        {
            new NodeModel(),
            new NodeModel()
        });
        diagram.Links.Add(new LinkModel(diagram.Nodes[0], diagram.Nodes[1])
        {
            Selected = true
        });

        // Act
        await KeyboardShortcutsDefaults.DeleteSelection(diagram);

        // Assert
        funcCalled.Should().BeTrue();
        diagram.Links.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSelection_ShouldResultInSingleRefresh()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.Nodes.Add(new NodeModel[]
        {
            new NodeModel { Selected = true },
            new NodeModel { Selected = true }
        });
        diagram.Links.Add(new LinkModel(diagram.Nodes[0], diagram.Nodes[1])
        {
            Selected = true
        });

        var refreshes = 0;
        diagram.Changed += () => refreshes++;

        // Act
        await KeyboardShortcutsDefaults.DeleteSelection(diagram);

        // Assert
        diagram.Nodes.Count.Should().Be(0);
        diagram.Links.Count.Should().Be(0);
        refreshes.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSelection_WhenAConstraintThrows_DeletesNothingAndLeavesRefreshOn()
    {
        // Arrange
        var diagram = new TestDiagram();
        diagram.Options.Constraints.ShouldDeleteNode = node =>
            node == diagram.Nodes[1] ? throw new InvalidOperationException() : ValueTask.FromResult(true);
        diagram.Nodes.Add(new NodeModel[] { new() { Selected = true }, new() { Selected = true } });
        var refreshes = 0;
        diagram.Changed += () => refreshes++;

        // Act
        var delete = async () => await KeyboardShortcutsDefaults.DeleteSelection(diagram);

        // Assert
        await delete.Should().ThrowAsync<InvalidOperationException>();
        diagram.Nodes.Count.Should().Be(2);
        diagram.SuspendRefresh.Should().BeFalse();
        refreshes.Should().Be(0);
    }

    [Fact]
    public async Task DeleteSelection_ShouldNotSuspendRefresh_WhileAConstraintIsAwaited()
    {
        // Arrange
        var diagram = new TestDiagram();
        var suspendedWhileAsked = new List<bool>();
        diagram.Options.Constraints.ShouldDeleteNode = async _ =>
        {
            await Task.Yield();
            suspendedWhileAsked.Add(diagram.SuspendRefresh);
            return true;
        };
        diagram.Nodes.Add(new NodeModel[] { new() { Selected = true }, new() { Selected = true } });

        // Act
        await KeyboardShortcutsDefaults.DeleteSelection(diagram);

        // Assert
        suspendedWhileAsked.Should().Equal(false, false);
        diagram.Nodes.Count.Should().Be(0);
    }
}
