using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Flow.Diagrams.Export;

/// <summary>
/// The diagram's SVG layer as a standalone document: the same renderers, order and label pass as the canvas, without
/// controls, widgets or HTML. Rendered statically by <see cref="DiagramSvgExporter"/>.
/// </summary>
public sealed class SvgExportCanvas : ComponentBase
{
    /// <summary>The diagram to draw; required.</summary>
    [Parameter] public BlazorDiagram Diagram { get; set; } = null!;
    /// <summary>The region of the diagram to draw, in diagram units; becomes the <c>viewBox</c> and, with <see cref="SvgExportOptions.Unit"/>, the document's size. Required.</summary>
    [Parameter] public Rectangle ViewBox { get; set; } = null!;
    /// <summary>Unit, background and embedded stylesheet of the document. Required.</summary>
    [Parameter] public SvgExportOptions Options { get; set; } = null!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var (x, y, w, h) = (ViewBox.Left, ViewBox.Top, ViewBox.Width, ViewBox.Height);
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "xmlns", "http://www.w3.org/2000/svg");
        builder.AddAttribute(2, "width", w.ToInvariantString() + Options.Unit);
        builder.AddAttribute(3, "height", h.ToInvariantString() + Options.Unit);
        builder.AddAttribute(4, "viewBox", FormattableString.Invariant($"{x} {y} {w} {h}"));

        if (Options.Css != null)
        {
            builder.OpenElement(5, "style");
            builder.AddContent(6, Options.Css);
            builder.CloseElement();
        }

        if (Options.Background != null)
        {
            builder.OpenElement(7, "rect");
            builder.AddAttribute(8, "x", x.ToInvariantString());
            builder.AddAttribute(9, "y", y.ToInvariantString());
            builder.AddAttribute(10, "width", w.ToInvariantString());
            builder.AddAttribute(11, "height", h.ToInvariantString());
            builder.AddAttribute(12, "fill", Options.Background);
            builder.CloseElement();
        }

        builder.OpenComponent<CascadingValue<BlazorDiagram>>(13);
        builder.AddAttribute(14, nameof(CascadingValue<BlazorDiagram>.Value), Diagram);
        builder.AddAttribute(15, nameof(CascadingValue<BlazorDiagram>.IsFixed), true);
        builder.AddAttribute(16, nameof(CascadingValue<BlazorDiagram>.ChildContent), (RenderFragment)BuildDrawing);
        builder.CloseComponent();

        builder.CloseElement();
    }

    private void BuildDrawing(RenderTreeBuilder builder)
    {
        foreach (var model in Diagram.OrderedSelectables)
        {
            switch (model)
            {
                case SvgNodeModel { Group: null } node:
                    builder.OpenComponent<NodeRenderer>(0);
                    builder.SetKey(node);
                    builder.AddAttribute(1, nameof(NodeRenderer.Node), node);
                    builder.CloseComponent();
                    break;
                case SvgGroupModel { Group: null } group:
                    builder.OpenComponent<GroupRenderer>(2);
                    builder.SetKey(group);
                    builder.AddAttribute(3, nameof(GroupRenderer.Group), group);
                    builder.CloseComponent();
                    break;
                case BaseLinkModel link:
                    builder.OpenComponent<LinkRenderer>(4);
                    builder.SetKey(link);
                    builder.AddAttribute(5, nameof(LinkRenderer.Link), link);
                    builder.CloseComponent();
                    break;
            }
        }

        if (!Diagram.Options.Links.LabelsOnTop)
            return;

        builder.OpenElement(6, "g");
        builder.AddAttribute(7, "class", "diagram-link-labels-layer");
        foreach (var link in Diagram.OrderedSelectables.OfType<BaseLinkModel>())
        {
            builder.OpenComponent<LinkLabelsRenderer>(8);
            builder.SetKey(link);
            builder.AddAttribute(9, nameof(LinkLabelsRenderer.Link), link);
            builder.CloseComponent();
        }
        builder.CloseElement();
    }
}
