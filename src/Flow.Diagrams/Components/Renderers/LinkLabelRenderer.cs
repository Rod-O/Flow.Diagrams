using System;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SvgPathProperties;

namespace Flow.Diagrams.Components.Renderers;

/// <summary>
/// Draws one link label at its place along the link's path: as SVG when <c>Options.Links.SvgLabels</c> is set, otherwise as
/// HTML inside a <c>foreignObject</c>. The label's <c>Distance</c> picks the place: 0 to 1 is a fraction of the path's
/// length, above 1 is a length from the start, below 0 a length back from the end, and <c>null</c> spreads the link's labels
/// evenly. Its <c>Offset</c> then moves it.
/// </summary>
public class LinkLabelRenderer : ComponentBase, IDisposable
{
    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;
    /// <summary>The label to draw; required.</summary>
    [Parameter] public LinkLabelModel Label { get; set; } = null!;
    /// <summary>The link's full path, which the label's position is measured along; required.</summary>
    [Parameter] public SvgPath Path { get; set; } = null!;

    /// <inheritdoc />
    public void Dispose()
    {
        Label.Changed -= OnLabelChanged;
        Label.VisibilityChanged -= OnLabelChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Label.Changed += OnLabelChanged;
        Label.VisibilityChanged += OnLabelChanged;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Label.Visible)
            return;
        
        var (x, y) = Place(Label, Path);

        if (BlazorDiagram.Options.Links.SvgLabels)
        {
            builder.OpenElement(0, "g");
            builder.AddAttribute(1, "class", "diagram-link-label");
            builder.AddAttribute(2, "transform", FormattableString.Invariant($"translate({x} {y})"));
            builder.OpenComponent(3, BlazorDiagram.GetComponent(Label) ?? typeof(DefaultSvgLinkLabelWidget));
            builder.AddAttribute(4, "Label", Label);
            builder.CloseComponent();
            builder.CloseElement();
            return;
        }

        builder.OpenElement(5, "foreignObject");
        builder.AddAttribute(6, "class", "diagram-link-label");
        builder.AddAttribute(7, "x", x.ToInvariantString());
        builder.AddAttribute(8, "y", y.ToInvariantString());
        builder.OpenComponent(9, BlazorDiagram.GetComponent(Label) ?? typeof(DefaultLinkLabelWidget));
        builder.AddAttribute(10, "Label", Label);
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void OnLabelChanged(Model model, ModelChanges changes) => OnLabelChanged(model);

    private void OnLabelChanged(Model _)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>Where <paramref name="label"/> is drawn along <paramref name="path"/>, its <c>Offset</c> included.</summary>
    internal static Point Place(LinkLabelModel label, SvgPath path)
    {
        var totalLength = path.Length;
        var length = label.Distance switch
        {
            <= 1 and >= 0 => label.Distance.Value * totalLength,
            > 1 => label.Distance.Value,
            < 0 => totalLength + label.Distance.Value,
            _ => totalLength * (label.Parent.Labels.IndexOf(label) + 1) / (label.Parent.Labels.Count + 1)
        };

        var pt = path.GetPointAtLength(length);
        return new Point(pt.X + (label.Offset?.X ?? 0), pt.Y + (label.Offset?.Y ?? 0));
    }
}