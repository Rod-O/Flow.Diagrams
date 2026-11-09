using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Events;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Behaviors;

public class KeyboardShortcutsBehaviorTests
{
    [Theory]
    [InlineData("A", true, true, true)]
    [InlineData("B", true, false, false)]
    [InlineData("C", true, true, false)]
    [InlineData("D", true, false, true)]
    [InlineData("E", false, false, true)]
    [InlineData("F", false, true, true)]
    [InlineData("G", false, false, false)]
    public void Behavior_ShouldExecuteAction_WhenCombinationIsPressed(string key, bool ctrl, bool shift, bool alt)
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        var executed = false;

        ksb.SetShortcut(key, ctrl, shift, alt, d =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        // Act
        diagram.TriggerKeyDown(new KeyboardEventArgs(key, key, 0, ctrl, shift, alt));

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void Behavior_ShouldDoNothing_WhenRemoved()
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        diagram.UnregisterBehavior<KeyboardShortcutsBehavior>();
        var executed = false;

        ksb.SetShortcut("A", false, false, false, d =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        // Act
        diagram.TriggerKeyDown(new KeyboardEventArgs("A", "A", 0, false, false, false));

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void SetShortcut_ShouldOverride()
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        var executed1 = false;
        var executed2 = false;

        ksb.SetShortcut("A", false, false, false, d =>
        {
            executed1 = true;
            return ValueTask.CompletedTask;
        });

        ksb.SetShortcut("A", false, false, false, d =>
        {
            executed2 = true;
            return ValueTask.CompletedTask;
        });

        // Act
        diagram.TriggerKeyDown(new KeyboardEventArgs("A", "A", 0, false, false, false));

        // Assert
        executed1.Should().BeFalse();
        executed2.Should().BeTrue();
    }
    [Fact]
    public void Behavior_ShouldExecuteAction_WhenShiftTurnsTheLetterToUpperCase()
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        var executed = false;
        ksb.SetShortcut("a", ctrl: true, shift: true, alt: false, d =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        // Act
        diagram.TriggerKeyDown(new KeyboardEventArgs("A", "KeyA", 0, true, true, false));

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void Behavior_ShouldExecuteMetaShortcut_OnlyWhenMetaIsHeld()
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        var executions = 0;
        ksb.SetShortcut("z", ctrl: false, shift: false, alt: false, meta: true, d =>
        {
            executions++;
            return ValueTask.CompletedTask;
        });

        // Act
        diagram.TriggerKeyDown(new KeyboardEventArgs("z", "KeyZ", 0, false, false, false, MetaKey: true));
        diagram.TriggerKeyDown(new KeyboardEventArgs("z", "KeyZ", 0, false, false, false));

        // Assert
        executions.Should().Be(1);
    }

    [Fact]
    public void Behavior_ShouldExecuteAction_WhenOptionChangesTheCharacterOnMacOS()
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        var executed = false;
        ksb.SetShortcut("g", ctrl: true, shift: false, alt: true, d =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        // Act
        diagram.TriggerKeyDown(new KeyboardEventArgs("\u00A9", "KeyG", 0, true, false, true));

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void Behavior_ShouldMatchTheCharacter_NotThePhysicalKey_WhenTheLayoutGivesALetter()
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        var executed = false;
        ksb.SetShortcut("a", ctrl: true, shift: false, alt: false, d =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        // Act: AZERTY, the physical A key types "q"
        diagram.TriggerKeyDown(new KeyboardEventArgs("q", "KeyA", 0, true, false, false));

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public async Task Behavior_ShouldRaiseActionFailed_WhenAnActionThrowsAfterAwaiting()
    {
        // Arrange
        var diagram = new TestDiagram();
        var ksb = diagram.GetBehavior<KeyboardShortcutsBehavior>()!;
        var failure = new TaskCompletionSource<Exception>();
        ksb.ActionFailed += ex => failure.TrySetResult(ex);
        ksb.SetShortcut("x", ctrl: false, shift: false, alt: false, async d =>
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        });

        // Act
        diagram.TriggerKeyDown(new KeyboardEventArgs("x", "KeyX", 0, false, false, false));
        var reported = await failure.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        reported.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("boom");
    }
}
