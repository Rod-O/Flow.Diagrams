using System;
using System.Threading.Tasks;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flow.Diagrams.Components;

/// <summary>
/// The surface a <see cref="Flow.Diagrams.BlazorDiagram"/> is drawn on: an SVG layer for links, SVG nodes and SVG groups, an HTML layer for
/// the other nodes and groups, and the controls and widgets on top. It forwards pointer, keyboard and wheel events to the
/// diagram, reports its own size to <c>BlazorDiagram.SetContainer</c>, and re-renders only when the diagram raises
/// <c>Changed</c>. Place it inside a <c>CascadingValue</c> of the diagram.
/// </summary>
public partial class DiagramCanvas : IAsyncDisposable
{
    private DotNetObjectReference<DiagramCanvas>? _reference;
    private bool _shouldRender;

    /// <summary>The canvas's root element, which is measured and observed for size changes.</summary>
    protected ElementReference elementReference;

    /// <summary>The diagram to draw; required, normally supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;

    /// <summary>Content drawn above both layers and outside their pan and zoom transform, such as <see cref="Widgets.GridWidget"/>, <see cref="Widgets.NavigatorWidget"/> or <see cref="Widgets.SelectionBoxWidget"/>.</summary>
    [Parameter] public RenderFragment? Widgets { get; set; }

    /// <summary>Extra content appended to the SVG layer, after the links and the SVG controls, in diagram coordinates.</summary>
    [Parameter] public RenderFragment? AdditionalSvg { get; set; }

    /// <summary>Extra content appended to the HTML layer, after the nodes and the HTML controls, in diagram coordinates.</summary>
    [Parameter] public RenderFragment? AdditionalHtml { get; set; }

    /// <summary>Extra CSS classes for the root element, added after <c>diagram-canvas</c>.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The runtime used to measure and observe the canvas; injected.</summary>
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            BlazorDiagram.Changed -= OnDiagramChanged;

            if (_reference == null)
                return;

            if (elementReference.Id != null && JSRuntime != null)
                await JSRuntime.UnobserveResizes(elementReference);

            _reference.Dispose();
        }
        catch (Exception ex) when (ex is JSDisconnectedException || ex is OperationCanceledException)
        {
            // This exception is expected when the user navigates away from the page
            // and the component is disposed. It can be ignored.
        }
    }

    private string GetLayerStyle(int order)
    {
        return FormattableString.Invariant(
            $"transform: translate({BlazorDiagram.Pan.X}px, {BlazorDiagram.Pan.Y}px) scale({BlazorDiagram.Zoom}); z-index: {order};");
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _reference = DotNetObjectReference.Create(this);
        BlazorDiagram.Changed += OnDiagramChanged;
    }

    /// <summary>On the first render, measures the canvas and starts observing its size.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                BlazorDiagram.SetContainer(await JSRuntime.GetBoundingClientRect(elementReference));
                await JSRuntime.ObserveResizes(elementReference, _reference!);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException || ex is OperationCanceledException)
        {
            // This exception is expected when the user navigates away from the page
            // and the component is disposed. It can be ignored
        }
    }

    /// <summary>Called from JavaScript when the canvas's size or position on the page changes; passes the new bounds, in screen pixels, to <c>BlazorDiagram.SetContainer</c>.</summary>
    [JSInvokable]
    public void OnResize(Rectangle rect)
    {
        BlazorDiagram.SetContainer(rect);
    }

    /// <summary>Renders only after the diagram has raised <c>Changed</c>; any other cause of a render pass is skipped.</summary>
    protected override bool ShouldRender()
    {
        if (!_shouldRender) return false;

        _shouldRender = false;
        return true;
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerDown(null, e.ToCore());
    }

    private void OnPointerMove(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerMove(null, e.ToCore());
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerUp(null, e.ToCore());
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        BlazorDiagram.TriggerKeyDown(e.ToCore());
    }

    private void OnWheel(WheelEventArgs e)
    {
        BlazorDiagram.TriggerWheel(e.ToCore());
    }

    private void OnDiagramChanged()
    {
        _shouldRender = true;
        InvokeAsync(StateHasChanged);
    }
}