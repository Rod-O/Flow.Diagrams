using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Models;
using Microsoft.AspNetCore.Components;
using System;
using Microsoft.AspNetCore.Components.Web;
using Flow.Diagrams.Extensions;
using Flow.Diagrams.Core.Extensions;
using Microsoft.AspNetCore.Components.Rendering;

namespace Flow.Diagrams.Components.Renderers;

/// <summary>
/// Draws one vertex of a link as a draggable handle: the widget registered for <c>LinkVertexModel</c>, or a circle of radius 5. It forwards
/// pointer events to the diagram, and double-clicking it removes the vertex with <see cref="BaseLinkModel.RemoveVertex"/>, which raises <see cref="BaseLinkModel.VertexRemoved"/>.
/// </summary>
public class LinkVertexRenderer : ComponentBase, IDisposable
{
    private bool _shouldRender;

    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;
    /// <summary>The vertex to draw; required.</summary>
    [Parameter] public LinkVertexModel Vertex { get; set; } = null!;
    /// <summary>CSS fill of the handle while the vertex is not selected.</summary>
    [Parameter] public string? Color { get; set; }
    /// <summary>CSS fill of the handle while the vertex is selected.</summary>
    [Parameter] public string? SelectedColor { get; set; }

    private string? ColorToUse => Vertex.Selected ? SelectedColor : Color;

    /// <inheritdoc />
    public void Dispose()
    {
        Vertex.Changed -= OnVertexChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Vertex.Changed += OnVertexChanged;
    }

    /// <summary>Renders only after the vertex changed; any other cause of a render pass is skipped.</summary>
    protected override bool ShouldRender() => _shouldRender;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Cleared here, not in ShouldRender: Blazor skips ShouldRender while a render is already queued.
        _shouldRender = false;

        var componentType = BlazorDiagram.GetComponent(Vertex);

        builder.OpenElement(0, "g");
        builder.AddAttribute(1, "class", "diagram-link-vertex");
        builder.AddAttribute(4, "cursor", "move");
        builder.AddAttribute(5, "ondblclick", value: EventCallback.Factory.Create<MouseEventArgs>(this, OnDoubleClick));
        builder.AddAttribute(6, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerDown));
        builder.AddAttribute(7, "onpointerup", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerUp));
        builder.AddEventStopPropagationAttribute(8, "onpointerdown", true);
        builder.AddEventStopPropagationAttribute(9, "onpointerup", true);

        if (componentType == null)
        {
            builder.OpenElement(10, "circle");
            builder.AddAttribute(11, "cx", Vertex.Position.X.ToInvariantString());
            builder.AddAttribute(12, "cy", Vertex.Position.Y.ToInvariantString());
            builder.AddAttribute(13, "r", "5");
            builder.AddAttribute(14, "fill", ColorToUse);
            builder.CloseElement();
        }
        else
        {
            builder.OpenComponent(15, componentType);
            builder.AddAttribute(16, "Vertex", Vertex);
            builder.AddAttribute(17, "Color", ColorToUse);
            builder.CloseComponent();
        }

        builder.CloseElement();
    }

    private void OnVertexChanged(Model _)
    {
        _shouldRender = true;
        InvokeAsync(StateHasChanged);
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerDown(Vertex, e.ToCore());
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerUp(Vertex, e.ToCore());
    }

    private void OnDoubleClick(MouseEventArgs e)
    {
        Vertex.Parent.RemoveVertex(Vertex);
        Vertex.Parent.Refresh();
    }
}
