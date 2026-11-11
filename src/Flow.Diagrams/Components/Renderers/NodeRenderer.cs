using System.Text;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Export;
using Flow.Diagrams.Extensions;
using Flow.Diagrams.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flow.Diagrams.Components.Renderers;

/// <summary>
/// Draws one node: a wrapper element (<c>g</c> for an <see cref="Models.SvgNodeModel"/>, <c>div</c> otherwise) positioned at the node's
/// position, holding the widget registered for the node's type, or a default widget. It forwards pointer events to the diagram,
/// and measures the node to set its <c>Size</c> unless the node has a controlled size or is drawn by <see cref="SvgExportCanvas"/>. Renders nothing while the node is invisible.
/// </summary>
public class NodeRenderer : ComponentBase, IDisposable
{
    private bool _becameVisible;
    private ElementReference _element;
    private bool _isSvg;
    private DotNetObjectReference<NodeRenderer>? _reference;
    private bool _shouldRender;

    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;

    /// <summary>The node to draw; required.</summary>
    [Parameter] public NodeModel Node { get; set; } = null!;

    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    [CascadingParameter] private SvgExportCanvas? ExportCanvas { get; set; }

    private bool ObservesResizes => !Node.ControlledSize && ExportCanvas == null;

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Node.Changed -= OnNodeChanged;
            Node.VisibilityChanged -= OnVisibilityChanged;

            if (_element.Id != null && ObservesResizes && JsRuntime != null)
            {
                _ = JsRuntime.UnobserveResizes(_element);
            }

            _reference?.Dispose();
        }
        catch (Exception ex) when (ex is JSDisconnectedException || ex is ObjectDisposedException)
        {
            // This exception can be ignored.
        }
    }

    /// <summary>
    /// Called from JavaScript when the node's element is resized. Stores the new size, converted from screen pixels to diagram units,
    /// and refreshes the node, its links and its ports; a zero size (the node is not rendered) and an unchanged size are ignored.
    /// </summary>
    [JSInvokable]
    public void OnResize(Size size)
    {
        // When the node becomes invisible (a.k.a unrendered), the size is zero
        if (Size.Zero.Equals(size))
            return;

        size = new Size(size.Width / BlazorDiagram.Zoom, size.Height / BlazorDiagram.Zoom);
        if (Node.Size != null && Node.Size.Width.AlmostEqualTo(size.Width) &&
            Node.Size.Height.AlmostEqualTo(size.Height))
        {
            return;
        }

        using (BlazorDiagram.Changes.Suspend())
            Node.SetSize(size);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _reference = DotNetObjectReference.Create(this);
        Node.Changed += OnNodeChanged;
        Node.VisibilityChanged += OnVisibilityChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        _isSvg = Node is SvgNodeModel;
    }

    /// <summary>Renders only after the node or its visibility changed; any other cause of a render pass is skipped.</summary>
    protected override bool ShouldRender() => _shouldRender;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Cleared here, not in ShouldRender: Blazor skips ShouldRender while a render is already queued.
        _shouldRender = false;

        if (!Node.Visible)
            return;

        var componentType = BlazorDiagram.GetComponent(Node) ??
                            (_isSvg ? typeof(SvgNodeWidget) : typeof(NodeWidget));
        var classes = new StringBuilder("diagram-node")
            .AppendIf(" locked", Node.Locked)
            .AppendIf(" selected", Node.Selected)
            .AppendIf(" grouped", Node.Group != null)
            .AppendIf(" " + Node.CssClass, !string.IsNullOrWhiteSpace(Node.CssClass));

        builder.OpenElement(0, _isSvg ? "g" : "div");
        builder.AddAttribute(1, "class", classes.ToString());
        builder.AddAttribute(2, "data-node-id", Node.Id);

        if (_isSvg)
        {
            builder.AddAttribute(3, "transform",
                $"translate({Node.Position.X.ToInvariantString()} {Node.Position.Y.ToInvariantString()})");
        }
        else
        {
            builder.AddAttribute(3, "style",
                $"top: {Node.Position.Y.ToInvariantString()}px; left: {Node.Position.X.ToInvariantString()}px");
        }

        builder.AddAttribute(4, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerDown));
        builder.AddEventStopPropagationAttribute(5, "onpointerdown", true);
        builder.AddAttribute(6, "onpointerup", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerUp));
        builder.AddEventStopPropagationAttribute(7, "onpointerup", true);
        builder.AddAttribute(8, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEnter));
        builder.AddAttribute(9, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseLeave));
        builder.AddElementReferenceCapture(10, value => _element = value);
        builder.OpenComponent(11, componentType);
        builder.AddAttribute(12, "Node", Node);
        builder.CloseComponent();

        builder.CloseElement();
    }

    /// <summary>Starts observing the element's size, on the first render or when the node becomes visible, unless the node has a controlled size or is being exported.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender && !Node.Visible)
                return;

            if (firstRender || _becameVisible)
            {
                _becameVisible = false;

                if (ObservesResizes)
                {
                    await JsRuntime.ObserveResizes(_element, _reference!);
                }
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException || ex is OperationCanceledException)
        {
            // This exception is expected when the user navigates away from the page
            // and the component is disposed. It can be ignored
        }
    }

    private void OnNodeChanged(Model model, ModelChanges changes) => OnNodeChanged(model);

    private void OnNodeChanged(Model _)
    {
        ReRender();
    }

    private void OnVisibilityChanged(Model _)
    {
        _becameVisible = Node.Visible;
        ReRender();
    }

    private void ReRender()
    {
        _shouldRender = true;
        InvokeAsync(StateHasChanged);
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerDown(Node, e.ToCore());
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerUp(Node, e.ToCore());
    }

    private void OnMouseEnter(MouseEventArgs e)
    {
        BlazorDiagram.TriggerPointerEnter(Node, e.ToCore());
    }

    private void OnMouseLeave(MouseEventArgs e)
    {
        BlazorDiagram.TriggerPointerLeave(Node, e.ToCore());
    }
}