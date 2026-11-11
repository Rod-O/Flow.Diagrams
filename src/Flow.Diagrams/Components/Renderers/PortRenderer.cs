using System;
using System.Linq;
using System.Threading.Tasks;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Extensions;
using Flow.Diagrams.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flow.Diagrams.Components.Renderers;

/// <summary>
/// Draws one port and measures it: after the first render it reads the element's bounds and stores the port's position
/// and size, in diagram units, which links use as their anchor. Use it inside a node or group widget, once per port. Renders nothing while the port is invisible.
/// </summary>
public class PortRenderer : ComponentBase, IDisposable
{
    private ElementReference _element;
    private bool _isParentSvg;
    private bool _shouldRefreshPort;
    private bool _shouldRender;
    private bool _updatingDimensions;

    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    /// <summary>The port to draw; required.</summary>
    [Parameter] public PortModel Port { get; set; } = null!;
    /// <summary>Extra CSS classes, added after <c>diagram-port</c>, the port's alignment and <c>has-links</c>.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Inline CSS for the port's element, typically to position it inside the node.</summary>
    [Parameter] public string? Style { get; set; }
    /// <summary>The port's visible content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Port.Changed -= OnPortChanged;
        Port.VisibilityChanged -= OnPortChanged;
        BlazorDiagram.ContainerChanged -= OnContainerChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        Port.Changed += OnPortChanged;
        Port.VisibilityChanged += OnPortChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        _isParentSvg = Port.Parent is SvgNodeModel;
    }

    /// <summary>Renders only after the port or its visibility changed; any other cause of a render pass is skipped.</summary>
    protected override bool ShouldRender() => _shouldRender;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Cleared here, not in ShouldRender: Blazor skips ShouldRender while a render is already queued.
        _shouldRender = false;

        if (!Port.Visible)
            return;
        
        builder.OpenElement(0, _isParentSvg ? "g" : "div");
        builder.AddAttribute(1, "style", Style);
        builder.AddAttribute(2, "class",
            "diagram-port" + " " + Port.Alignment.ToString().ToLower() + " " + (Port.Links.Count > 0 ? "has-links" : "") + " " +
            Class);
        builder.AddAttribute(3, "data-port-id", Port.Id);
        builder.AddAttribute(4, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerDown));
        builder.AddEventStopPropagationAttribute(5, "onpointerdown", true);
        builder.AddAttribute(6, "onpointerup", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerUp));
        builder.AddEventStopPropagationAttribute(7, "onpointerup", true);
        builder.AddElementReferenceCapture(8, __value => { _element = __value; });
        builder.AddContent(9, ChildContent);
        builder.CloseElement();
    }

    /// <summary>While the port has not been measured, measures it; if the canvas is not measured yet, waits for it.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (!Port.Initialized)
            {
                await UpdateDimensions();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException || ex is OperationCanceledException)
        {
            // This exception is expected when the user navigates away from the page
            // and the component is disposed. It can be ignored
        }
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerDown(Port, e.ToCore());
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        var model = e.PointerType == "mouse" ? Port : FindPortOn(e.ClientX, e.ClientY);
        BlazorDiagram.TriggerPointerUp(model, e.ToCore());
    }

    private void OnContainerChanged()
    {
        BlazorDiagram.ContainerChanged -= OnContainerChanged;
        _shouldRender = true;
        InvokeAsync(StateHasChanged);
    }

    private PortModel? FindPortOn(double clientX, double clientY)
    {
        var relativePt = BlazorDiagram.GetRelativeMousePoint(clientX, clientY);
        var allPorts = BlazorDiagram.Nodes.SelectMany(n => n.Ports)
            .Union(BlazorDiagram.Groups.SelectMany(g => g.Ports));

        foreach (var port in allPorts)
        {
            if (!port.Initialized)
                continue;

            if (port.GetBounds().ContainsPoint(relativePt))
                return port;
        }

        return null;
    }

    private async Task UpdateDimensions()
    {
        if (BlazorDiagram.Container == null)
        {
            // Positions are relative to the canvas, which is not measured yet: measure once it is. Waiting for nothing
            // left the port unmeasured for good whenever nothing else asked again (a node whose size the model controls).
            BlazorDiagram.ContainerChanged -= OnContainerChanged;
            BlazorDiagram.ContainerChanged += OnContainerChanged;
            return;
        }

        _updatingDimensions = true;
        var zoom = BlazorDiagram.Zoom;
        var pan = BlazorDiagram.Pan;
        var rect = await JSRuntime.GetBoundingClientRect(_element);

        // Initialized first: the marks of the setters below must not start another measurement.
        Port.Initialized = true;
        _updatingDimensions = false;
        var changedMeanwhile = _shouldRefreshPort;
        _shouldRefreshPort = false;

        using (Port.Suspend())
        {
            if (changedMeanwhile)
                Port.MarkChanged(ModelChanges.Appearance);

            Port.Size = new Size(rect.Width / zoom, rect.Height / zoom);
            Port.Position = new Point((rect.Left - BlazorDiagram.Container.Left - pan.X) / zoom,
                (rect.Top - BlazorDiagram.Container.Top - pan.Y) / zoom);
        }
    }

    private void OnPortChanged(Model model, ModelChanges changes) => OnPortChanged(model);

    private async void OnPortChanged(Model _)
    {
        // If an update is ongoing and the port is refreshed again,
        // it's highly likely the port needs to be refreshed (e.g. link added)
        if (_updatingDimensions) _shouldRefreshPort = true;

        if (Port.Initialized)
        {
            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            await UpdateDimensions();
        }
    }
}
