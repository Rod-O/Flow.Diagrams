using System.Runtime.CompilerServices;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Options;
using Flow.Diagrams.Core.PathGenerators;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.Memory;

/// <summary>
/// Models removed from a live diagram, behaviors unregistered from it, and dropped diagrams must not stay reachable
/// through event subscriptions or remembered state. Every diagram here has the default behaviors plus the opt-in ones
/// (<see cref="LinkHalosBehavior"/>, <see cref="DebugEventsBehavior"/>) and virtualization enabled.
/// </summary>
[Trait("Category", "Memory")]
public class DiagramLeakTests
{
    [Fact]
    public void RemovedNode_WithPortsLinksGroupAndControls_ShouldBeCollectable()
    {
        var diagram = FullDiagram();

        var node = AddThenRemove(diagram, d => d.Nodes.Remove, (d, n) => { });

        Gc.IsCollected(node).Should().BeTrue();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void RemovedNode_AfterBeingClicked_ShouldBeCollectable()
    {
        var diagram = FullDiagram();

        var node = AddThenRemove(diagram, d => d.Nodes.Remove, (d, n) =>
        {
            d.TriggerPointerDown(n, Pointer(10, 10));
            d.TriggerPointerUp(n, Pointer(10, 10));
        });

        Gc.IsCollected(node).Should().BeTrue();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void RemovedNode_AfterBeingDragged_ShouldBeCollectable()
    {
        var diagram = FullDiagram();

        var node = AddThenRemove(diagram, d => d.Nodes.Remove, (d, n) =>
        {
            d.SelectModel(n, unselectOthers: true);
            d.TriggerPointerDown(n, Pointer(10, 10));
            d.TriggerPointerMove(n, Pointer(40, 30));
            d.TriggerPointerUp(n, Pointer(40, 30));
        });

        Gc.IsCollected(node).Should().BeTrue();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void RemovedLink_ShouldBeCollectable()
    {
        var diagram = FullDiagram();

        var link = AddLinkThenRemove(diagram);

        Gc.IsCollected(link).Should().BeTrue();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void RemovedGroup_ShouldBeCollectable_WhileItsChildrenStay()
    {
        var diagram = FullDiagram();

        var group = AddGroupThenRemove(diagram, delete: false);

        Gc.IsCollected(group).Should().BeTrue();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void DeletedGroup_ShouldBeCollectable_WithItsChildren()
    {
        var diagram = FullDiagram();

        var group = AddGroupThenRemove(diagram, delete: true);

        Gc.IsCollected(group).Should().BeTrue();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void RemovedGroup_ShouldBeCollectable_AfterAChildWasTakenOutOfItAndKept()
    {
        var diagram = FullDiagram();

        var group = RemoveChildThenGroup(diagram);

        Gc.IsCollected(group).Should().BeTrue();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void DroppedDiagram_ShouldBeCollectable_AfterItsModelsMovedToAnotherDiagram()
    {
        var target = new TestDiagram();

        var references = MoveModelsThenDrop(target);

        Gc.Uncollected(references).Should().BeEmpty();
        target.Nodes.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(typeof(SelectionBehavior))]
    [InlineData(typeof(DragMovablesBehavior))]
    [InlineData(typeof(DragNewLinkBehavior))]
    [InlineData(typeof(PanBehavior))]
    [InlineData(typeof(ZoomBehavior))]
    [InlineData(typeof(EventsBehavior))]
    [InlineData(typeof(KeyboardShortcutsBehavior))]
    [InlineData(typeof(Core.Controls.ControlsBehavior))]
    [InlineData(typeof(VirtualizationBehavior))]
    [InlineData(typeof(LinkHalosBehavior))]
    [InlineData(typeof(DebugEventsBehavior))]
    public void UnregisteredBehavior_ShouldBeCollectable(Type behaviorType)
    {
        var diagram = FullDiagram();

        var behavior = Unregister(diagram, behaviorType);

        Gc.IsCollected(behavior).Should().BeTrue();
        diagram.Nodes.Should().NotBeEmpty();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void DroppedDiagram_ShouldBeCollectable_WithItsModelsAndBehaviors()
    {
        var references = CreateAndDropDiagram();

        Gc.Uncollected(references).Should().BeEmpty();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddThenRemove(TestDiagram diagram, Func<Diagram, Action<NodeModel>> remove,
        Action<Diagram, NodeModel> interact)
    {
        var node = diagram.Nodes.Add(new NodeModel(new Point(5, 5)) { Size = new Size(50, 50) });
        var port = node.AddPort(PortAlignment.Right);
        var other = diagram.Nodes[0];
        diagram.Links.Add(new LinkModel(port, other.Ports[0]));
        diagram.Links.Add(new LinkModel(other, node));
        diagram.Groups[0].AddChild(node);
        diagram.Controls.AddFor(node).Add(new RemoveControl(0, 0));
        diagram.SelectModel(node, unselectOthers: false);
        interact(diagram, node);

        remove(diagram)(node);
        return new WeakReference(node);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddLinkThenRemove(TestDiagram diagram)
    {
        var link = diagram.Links.Add(new LinkModel(diagram.Nodes[0].Ports[0], diagram.Nodes[2].Ports[0]));
        link.AddLabel("label");
        link.AddVertex(new Point(300, 300));
        diagram.Controls.AddFor(link).Add(new RemoveControl(0, 0));
        diagram.SelectModel(link, unselectOthers: true);
        diagram.TriggerPointerDown(link, Pointer(10, 10));
        diagram.TriggerPointerUp(link, Pointer(10, 10));

        diagram.Links.Remove(link);
        return new WeakReference(link);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddGroupThenRemove(TestDiagram diagram, bool delete)
    {
        var a = diagram.Nodes.Add(new NodeModel(new Point(600, 600)) { Size = new Size(50, 50) });
        var b = diagram.Nodes.Add(new NodeModel(new Point(700, 600)) { Size = new Size(50, 50) });
        var group = diagram.Groups.Group(a, b);
        var port = group.AddPort(PortAlignment.Left);
        diagram.Links.Add(new LinkModel(port, diagram.Nodes[0].Ports[0]));
        diagram.Controls.AddFor(group).Add(new RemoveControl(0, 0));
        diagram.SelectModel(group, unselectOthers: true);
        diagram.TriggerPointerDown(group, Pointer(10, 10));
        diagram.TriggerPointerMove(group, Pointer(30, 10));
        diagram.TriggerPointerUp(group, Pointer(30, 10));

        if (delete)
            diagram.Groups.Delete(group);
        else
            diagram.Groups.Remove(group);
        return new WeakReference(group);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RemoveChildThenGroup(TestDiagram diagram)
    {
        var group = diagram.Groups[0];
        group.RemoveChild(group.Children[0]);
        diagram.Groups.Remove(group);
        return new WeakReference(group);
    }

    /// <summary>Takes every model out of a full diagram, adds the nodes and links to <paramref name="target"/> and drops the source.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(string, WeakReference)> MoveModelsThenDrop(TestDiagram target)
    {
        var source = FullDiagram();
        var nodes = source.Nodes.ToList();
        var links = source.Links.ToList();
        source.Groups.Clear();
        source.Links.Clear();
        source.Nodes.Clear();

        target.Nodes.Add(nodes);
        target.Links.Add(links);
        return new List<(string, WeakReference)>
        {
            ("source diagram", new WeakReference(source)),
            ("source controls layer", new WeakReference(source.Controls)),
            ("source virtualization behavior", new WeakReference(source.GetBehavior<VirtualizationBehavior>()!)),
            ("source halos behavior", new WeakReference(source.GetBehavior<LinkHalosBehavior>()!)),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Unregister(TestDiagram diagram, Type behaviorType)
    {
        var behavior = typeof(Diagram).GetMethod(nameof(Diagram.GetBehavior))!.MakeGenericMethod(behaviorType)
            .Invoke(diagram, null)!;
        typeof(Diagram).GetMethod(nameof(Diagram.UnregisterBehavior))!.MakeGenericMethod(behaviorType)
            .Invoke(diagram, null);
        return new WeakReference(behavior);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(string, WeakReference)> CreateAndDropDiagram()
    {
        var diagram = FullDiagram();
        return new List<(string, WeakReference)>
        {
            ("diagram", new WeakReference(diagram)),
            ("node", new WeakReference(diagram.Nodes[0])),
            ("link", new WeakReference(diagram.Links[0])),
            ("group", new WeakReference(diagram.Groups[0])),
            ("halos behavior", new WeakReference(diagram.GetBehavior<LinkHalosBehavior>()!)),
            ("virtualization behavior", new WeakReference(diagram.GetBehavior<VirtualizationBehavior>()!)),
        };
    }

    /// <summary>
    /// Three nodes with ports and crossing straight links, the first two grouped, controls on the first node; every
    /// behavior registered, virtualization on, one node off-screen, and a click and a drag already handled.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TestDiagram FullDiagram()
    {
        var diagram = new TestDiagram(new DiagramOptions { Virtualization = { Enabled = true } });
        diagram.Options.Links.DefaultPathGenerator = new StraightPathGenerator();
        diagram.SetContainer(new Rectangle(0, 0, 800, 600));
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));
        diagram.RegisterBehavior(new DebugEventsBehavior(diagram));

        var nodes = new[]
        {
            new NodeModel(new Point(0, 0)), new NodeModel(new Point(200, 200)), new NodeModel(new Point(5000, 0)),
        };
        foreach (var node in nodes)
        {
            node.Size = new Size(80, 40);
            node.AddPort(PortAlignment.Top);
            node.AddPort(PortAlignment.Bottom);
            diagram.Nodes.Add(node);
        }

        diagram.Links.Add(new LinkModel(nodes[0].Ports[0], nodes[1].Ports[1]));
        diagram.Links.Add(new LinkModel(nodes[1].Ports[0], nodes[2].Ports[1]));
        diagram.Links.Add(new LinkModel(nodes[0], nodes[2]));
        diagram.Groups.Group(nodes[0], nodes[1]);
        diagram.Controls.AddFor(nodes[0]).Add(new RemoveControl(0, 0));

        diagram.SelectModel(nodes[1], unselectOthers: true);
        diagram.TriggerPointerDown(nodes[1], Pointer(0, 0));
        diagram.TriggerPointerMove(nodes[1], Pointer(10, 0));
        diagram.TriggerPointerUp(nodes[1], Pointer(10, 0));
        diagram.TriggerPointerDown(nodes[0], Pointer(0, 0));
        diagram.TriggerPointerUp(nodes[0], Pointer(0, 0));
        return diagram;
    }

    private static PointerEventArgs Pointer(double x, double y) =>
        new(x, y, 0, 0, false, false, false, 0, 0, 0, 0, 0, 0, "mouse", true);
}
