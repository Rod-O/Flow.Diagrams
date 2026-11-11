using System;
using System.Text;
using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Flow.Diagrams.Components.Renderers;

/// <summary>
/// Draws one link: a <c>g</c> holding the link's halos and the widget registered for the link's type, or <see cref="LinkWidget"/>
/// for a <see cref="LinkModel"/>. A link of any other type without a registered widget throws a <see cref="DiagramsException"/>.
/// It forwards pointer events to the diagram. Renders nothing while the link is invisible.
/// </summary>
public class LinkRenderer : ComponentBase, IDisposable
{
    private bool _shouldRender;

    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;

    /// <summary>The link to draw; required.</summary>
    [Parameter] public BaseLinkModel Link { get; set; } = null!;

    /// <inheritdoc />
    public void Dispose()
    {
        Link.Changed -= OnLinkChanged;
        Link.VisibilityChanged -= OnLinkChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        Link.Changed += OnLinkChanged;
        Link.VisibilityChanged += OnLinkChanged;
    }

    /// <summary>Renders only after the link or its visibility changed; any other cause of a render pass is skipped.</summary>
    protected override bool ShouldRender() => _shouldRender;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Cleared here, not in ShouldRender: Blazor skips ShouldRender while a render is already queued.
        _shouldRender = false;

        if (!Link.Visible)
            return;
        
        var componentType = BlazorDiagram.GetComponent(Link) ?? (Link is LinkModel
            ? typeof(LinkWidget)
            : throw new DiagramsException(
                $"No widget is registered for the link type '{Link.GetType().Name}', and the default {nameof(LinkWidget)} draws only a {nameof(LinkModel)}. Register one with {nameof(BlazorDiagram)}.{nameof(BlazorDiagram.RegisterComponent)}."));
        var classes = new StringBuilder()
            .Append("diagram-link")
            .AppendIf(" attached", Link.IsAttached)
            .AppendIf(" " + Link.CssClass, !string.IsNullOrWhiteSpace(Link.CssClass))
            .ToString();

        builder.OpenElement(0, "g");
        builder.AddAttribute(1, "class", classes);
        builder.AddAttribute(2, "data-link-id", Link.Id);
        builder.AddAttribute(3, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerDown));
        builder.AddEventStopPropagationAttribute(4, "onpointerdown", true);
        builder.AddAttribute(5, "onpointerup", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerUp));
        builder.AddEventStopPropagationAttribute(6, "onpointerup", true);
        builder.AddAttribute(7, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEnter));
        builder.AddAttribute(8, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseLeave));
        builder.OpenRegion(9);
        foreach (var halo in Link.Halos)
            BuildHalo(builder, halo);
        builder.CloseRegion();
        builder.OpenComponent(10, componentType);
        builder.AddAttribute(11, "Link", Link);
        builder.CloseComponent();
        builder.CloseElement();
    }

    // Drawn before the link's widget: above the links below it, under its own stroke.
    private void BuildHalo(RenderTreeBuilder builder, LinkHalo halo)
    {
        builder.OpenElement(0, "path");
        builder.AddAttribute(1, "class", "diagram-link-halo");
        builder.AddAttribute(2, "d", FormattableString.Invariant($"M {halo.Start.X} {halo.Start.Y} L {halo.End.X} {halo.End.Y}"));
        builder.AddAttribute(3, "stroke", BlazorDiagram.Options.Links.HaloColor);
        builder.AddAttribute(4, "stroke-width", halo.Width.ToInvariantString());
        builder.AddAttribute(5, "fill", "none");
        builder.AddAttribute(6, "pointer-events", "none");
        builder.CloseElement();
    }

    private void OnLinkChanged(Model model, ModelChanges changes) => OnLinkChanged(model);

    private void OnLinkChanged(Model _)
    {
        _shouldRender = true;
        InvokeAsync(StateHasChanged);
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerDown(Link, e.ToCore());
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        BlazorDiagram.TriggerPointerUp(Link, e.ToCore());
    }

    private void OnMouseEnter(MouseEventArgs e)
    {
        BlazorDiagram.TriggerPointerEnter(Link, e.ToCore());
    }

    private void OnMouseLeave(MouseEventArgs e)
    {
        BlazorDiagram.TriggerPointerLeave(Link, e.ToCore());
    }
}