using System.Runtime.CompilerServices;
using Bunit;
using Flow.Diagrams.Components;
using Flow.Diagrams.Components.Controls;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Components.Widgets;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Models;
using Flow.Diagrams.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Flow.Diagrams.Tests.Memory;

/// <summary>
/// A rendered canvas must not keep a diagram alive, a diagram must not keep a disposed canvas alive, and a model removed
/// from a rendered diagram must leave neither itself nor the components that drew it reachable.
/// </summary>
[Trait("Category", "Memory")]
public class CanvasLeakTests
{
    [Fact]
    public void DisposedCanvas_ShouldBeCollectable_WhileTheDiagramLivesOn()
    {
        var diagram = FullDiagram();

        var components = RenderThenDispose(diagram);

        Gc.Uncollected(components).Should().BeEmpty();
        GC.KeepAlive(diagram);
    }

    [Fact]
    public void DroppedContext_ShouldLeaveTheDiagramAndItsModelsCollectable()
    {
        var references = RenderThenDropEverything();

        Gc.Uncollected(references).Should().BeEmpty();
    }

    [Fact]
    public void NodeRemovedFromARenderedDiagram_ShouldBeCollectable_WithItsRendererAndWidget()
    {
        using var ctx = CreateContext();
        var diagram = FullDiagram();
        var cut = ctx.RenderComponent<CanvasHost>(p => p.Add(h => h.Diagram, diagram));

        var references = RemoveRendered<NodeRenderer, NodeWidget>(cut, diagram,
            d => d.Nodes.First(n => n is not SvgNodeModel && n.Group == null), (d, m) => d.Nodes.Remove((NodeModel)m),
            r => r.Node, w => w.Node);

        Gc.Uncollected(references).Should().BeEmpty();
        cut.FindComponents<NodeRenderer>().Should().NotBeEmpty();
    }

    [Fact]
    public void SvgNodeRemovedFromARenderedDiagram_ShouldBeCollectable_WithItsRendererAndWidget()
    {
        using var ctx = CreateContext();
        var diagram = FullDiagram();
        var cut = ctx.RenderComponent<CanvasHost>(p => p.Add(h => h.Diagram, diagram));

        var references = RemoveRendered<NodeRenderer, SvgNodeWidget>(cut, diagram,
            d => d.Nodes.OfType<SvgNodeModel>().First(), (d, m) => d.Nodes.Remove((NodeModel)m),
            r => r.Node, w => w.Node);

        Gc.Uncollected(references).Should().BeEmpty();
    }

    [Fact]
    public void LinkRemovedFromARenderedDiagram_ShouldBeCollectable_WithItsRendererAndWidget()
    {
        using var ctx = CreateContext();
        var diagram = FullDiagram();
        var cut = ctx.RenderComponent<CanvasHost>(p => p.Add(h => h.Diagram, diagram));

        var references = RemoveRendered<LinkRenderer, LinkWidget>(cut, diagram,
            d => d.Links.First(l => l.Labels.Count > 0), (d, m) => d.Links.Remove((BaseLinkModel)m),
            r => r.Link, w => w.Link);

        Gc.Uncollected(references).Should().BeEmpty();
    }

    [Fact]
    public void GroupRemovedFromARenderedDiagram_ShouldBeCollectable_WithItsRendererAndWidget()
    {
        using var ctx = CreateContext();
        var diagram = FullDiagram();
        var cut = ctx.RenderComponent<CanvasHost>(p => p.Add(h => h.Diagram, diagram));

        var references = RemoveRendered<GroupRenderer, DefaultGroupWidget>(cut, diagram,
            d => d.Groups.First(), (d, m) => d.Groups.Delete((GroupModel)m),
            r => r.Group, w => w.Group);

        Gc.Uncollected(references).Should().BeEmpty();
    }

    [Fact]
    public void PortRendererDisposedBeforeTheCanvasWasMeasured_ShouldBeCollectable_WhileTheDiagramLivesOn()
    {
        var diagram = FullDiagram();
        diagram.Container.Should().BeNull();

        var ports = RenderNodeWithoutCanvasThenDispose(diagram);

        Gc.Uncollected(ports).Should().BeEmpty();
        GC.KeepAlive(diagram);
    }

    /// <summary>Without a canvas the diagram's container is never set, so each port waits for it on <c>ContainerChanged</c>.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(string, WeakReference)> RenderNodeWithoutCanvasThenDispose(BlazorDiagram diagram)
    {
        var ctx = CreateContext();
        var cut = ctx.RenderComponent<NodeRenderer>(p => p
            .Add(r => r.Node, diagram.Nodes[0])
            .AddCascadingValue(diagram));
        var references = cut.FindComponents<PortRenderer>()
            .Select(c => (nameof(PortRenderer), new WeakReference(c.Instance))).ToList();
        references.Should().NotBeEmpty();
        ctx.Dispose();
        return references;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(string, WeakReference)> RenderThenDispose(BlazorDiagram diagram)
    {
        var ctx = CreateContext();
        var cut = ctx.RenderComponent<CanvasHost>(p => p.Add(h => h.Diagram, diagram));
        var references = Instances(cut);
        references.Select(r => r.Item1).Distinct().Should().HaveCount(17, "every component type should be drawn");
        ctx.Dispose();
        return references;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(string, WeakReference)> RenderThenDropEverything()
    {
        var ctx = CreateContext();
        var diagram = FullDiagram();
        var cut = ctx.RenderComponent<CanvasHost>(p => p.Add(h => h.Diagram, diagram));
        var references = Instances(cut);
        references.Add(("diagram", new WeakReference(diagram)));
        references.AddRange(diagram.Nodes.Concat(diagram.Groups).Select(n => ("node", new WeakReference(n))));
        references.AddRange(diagram.Links.Select(l => ("link", new WeakReference(l))));
        ctx.Dispose();
        return references;
    }

    /// <summary>
    /// Removes the model <paramref name="pick"/> selects, after checking a renderer and a widget draw it, and returns weak
    /// references to all three.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(string, WeakReference)> RemoveRendered<TRenderer, TWidget>(IRenderedComponent<CanvasHost> cut,
        BlazorDiagram diagram, Func<BlazorDiagram, Model> pick, Action<BlazorDiagram, Model> remove,
        Func<TRenderer, Model> rendererModel, Func<TWidget, Model> widgetModel)
        where TRenderer : IComponent where TWidget : IComponent
    {
        var model = pick(diagram);
        var renderer = cut.FindComponents<TRenderer>().Single(r => rendererModel(r.Instance) == model).Instance;
        var widget = cut.FindComponents<TWidget>().Single(w => widgetModel(w.Instance) == model).Instance;

        cut.InvokeAsync(() => remove(diagram, model)).Wait();
        RenderEveryLayerTwice(cut, diagram);

        cut.FindComponents<TRenderer>().Should().NotContain(r => ReferenceEquals(r.Instance, renderer));
        return new List<(string, WeakReference)>
        {
            ("model", new WeakReference(model)),
            (typeof(TRenderer).Name, new WeakReference(renderer)),
            (typeof(TWidget).Name, new WeakReference(widget)),
        };
    }

    /// <summary>
    /// Blazor keeps each component's previous render tree, with the parameters and keys it held, until the component
    /// renders again. A removed model therefore stays reachable from the canvas and the controls layers for one more
    /// render of each, which in an app is the next change; this makes that next render happen.
    /// </summary>
    private static void RenderEveryLayerTwice(IRenderedComponent<CanvasHost> cut, BlazorDiagram diagram)
    {
        cut.InvokeAsync(diagram.Refresh).Wait();
        cut.InvokeAsync(diagram.Refresh).Wait();
        foreach (var controls in diagram.Controls.Models.Select(m => diagram.Controls.GetFor(m)!).ToList())
        {
            var visible = controls.Visible;
            cut.InvokeAsync(() => { if (visible) controls.Hide(); else controls.Show(); }).Wait();
            cut.InvokeAsync(() => { if (visible) controls.Show(); else controls.Hide(); }).Wait();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(string, WeakReference)> Instances(IRenderedComponent<CanvasHost> cut)
    {
        var references = new List<(string, WeakReference)>();
        void Add<T>() where T : IComponent =>
            references.AddRange(cut.FindComponents<T>().Select(c => (typeof(T).Name, new WeakReference(c.Instance))));

        Add<DiagramCanvas>();
        Add<NodeRenderer>();
        Add<GroupRenderer>();
        Add<LinkRenderer>();
        Add<LinkLabelsRenderer>();
        Add<LinkLabelRenderer>();
        Add<LinkVertexRenderer>();
        Add<PortRenderer>();
        Add<NodeWidget>();
        Add<SvgNodeWidget>();
        Add<DefaultGroupWidget>();
        Add<LinkWidget>();
        Add<ControlsLayerRenderer>();
        Add<RemoveControlWidget>();
        Add<NavigatorWidget>();
        Add<GridWidget>();
        Add<SelectionBoxWidget>();
        return references;
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IJSRuntime>(new InertJSRuntime());
        return ctx;
    }

    /// <summary>
    /// HTML and SVG nodes with ports and controls (shown on the selected one), a group, links with a label (drawn on top) and a vertex; every behavior
    /// registered and virtualization on.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BlazorDiagram FullDiagram()
    {
        var diagram = new BlazorDiagram(new BlazorDiagramOptions
        {
            Virtualization = { Enabled = true },
            Links = { LabelsOnTop = true },
        });
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));

        var html = new[] { new NodeModel(new Point(0, 0)), new NodeModel(new Point(200, 0)), new NodeModel(new Point(400, 0)) };
        var svg = new[] { new SvgNodeModel(new Point(0, 300)), new SvgNodeModel(new Point(200, 300)) };
        foreach (var node in html.Concat<NodeModel>(svg))
        {
            node.Size = new Size(80, 40);
            node.AddPort(PortAlignment.Top);
            node.AddPort(PortAlignment.Bottom);
            diagram.Nodes.Add(node);
        }

        diagram.Groups.Group(html[1], html[2]);
        var link = diagram.Links.Add(new LinkModel(html[0], svg[0]));
        link.AddLabel("label");
        link.AddVertex(new Point(50, 150));
        diagram.Links.Add(new LinkModel(svg[0].Ports[1], svg[1].Ports[0]));
        diagram.Links.Add(new LinkModel(html[0].Ports[1], html[1].Ports[0]));

        foreach (var node in html.Concat<NodeModel>(svg))
            diagram.Controls.AddFor(node).Add(new RemoveControl(0, 0));
        diagram.SelectModel(html[0], unselectOthers: true);
        return diagram;
    }
}
