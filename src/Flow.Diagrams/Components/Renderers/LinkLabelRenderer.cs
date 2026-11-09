using System;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SvgPathProperties;

namespace Flow.Diagrams.Components.Renderers;

public class LinkLabelRenderer : ComponentBase, IDisposable
{
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;
    [Parameter] public LinkLabelModel Label { get; set; } = null!;
    [Parameter] public SvgPath Path { get; set; } = null!;

    public void Dispose()
    {
        Label.Changed -= OnLabelChanged;
        Label.VisibilityChanged -= OnLabelChanged;
    }

    protected override void OnInitialized()
    {
        Label.Changed += OnLabelChanged;
        Label.VisibilityChanged += OnLabelChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Label.Visible)
            return;
        
        var position = FindPosition();
        if (position == null)
            return;
        
        var x = position.X + (Label.Offset?.X ?? 0);
        var y = position.Y + (Label.Offset?.Y ?? 0);

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

    private void OnLabelChanged(Model _)
    {
        InvokeAsync(StateHasChanged);
    }

    private Point? FindPosition()
    {
        var totalLength = Path.Length;
        var length = Label.Distance switch
        {
            <= 1 and >= 0 => Label.Distance.Value * totalLength,
            > 1 => Label.Distance.Value,
            < 0 => totalLength + Label.Distance.Value,
            _ => totalLength * (Label.Parent.Labels.IndexOf(Label) + 1) / (Label.Parent.Labels.Count + 1)
        };

        var pt = Path.GetPointAtLength(length);
        return new Point(pt.X, pt.Y);
    }
}