using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Export;
using Flow.Diagrams.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Flow.Diagrams.Components;

/// <summary>
/// The default widget for a <see cref="LinkModel"/>: its path, markers, vertex handles and, unless
/// <c>LabelsOnTop</c> is set, its labels. It also draws a wide transparent helper path that makes the link easy to pick and,
/// for a segmentable link, to drag a new vertex out of. Vertex handles and new vertices are offered only when the link's
/// router <see cref="Core.Routers.Router.FollowsVertices"/>. A link with a <see cref="LinkModel.Flow"/> gets a dashed stroke whose
/// offset the stylesheet's <c>diagram-link-flow</c> animation moves; the SVG export draws it solid.
/// </summary>
public partial class LinkWidget
{
    private bool _hovered;

    /// <summary>The diagram the link belongs to.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;
    /// <summary>The link to draw. A custom link type that derives from <c>BaseLinkModel</c> but not <see cref="LinkModel"/> needs its own widget registered.</summary>
    [Parameter] public LinkModel Link { get; set; } = null!;

    [CascadingParameter] private SvgExportCanvas? ExportCanvas { get; set; }

    // The export is a still document: a flowing link is drawn as if it had no flow.
    private LinkFlow? DrawnFlow => ExportCanvas == null ? Link.Flow : null;

    private bool FollowsVertices => (Link.Router ?? BlazorDiagram.Options.Links.DefaultRouter).FollowsVertices;
    private bool CreatesVertices => Link.Segmentable && FollowsVertices;

    // One keyframes rule serves every link: it moves the dash offset by one period, in the period's time at this speed.
    private static string? FlowStyle(LinkFlow flow)
    {
        if (flow.Speed <= 0)
            return null;

        var period = flow.Dash + flow.Gap;
        var style = FormattableString.Invariant($"--diagram-flow-period:{period}px;--diagram-flow-duration:{period / flow.Speed}s");
        return flow.Reverse ? style + ";animation-direction:reverse" : style;
    }

    private RenderFragment GetSelectionHelperPath(string color, string d, int index)
    {
        return builder =>
        {
            builder.OpenElement(0, "path");
            builder.AddAttribute(1, "class", "selection-helper");
            builder.AddAttribute(2, "stroke", color);
            builder.AddAttribute(3, "stroke-width", 12);
            builder.AddAttribute(4, "d", d);
            builder.AddAttribute(5, "stroke-linecap", "butt");
            builder.AddAttribute(6, "stroke-opacity", _hovered ? "0.05" : "0");
            builder.AddAttribute(7, "fill", "none");
            builder.AddAttribute(8, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEnter));
            builder.AddAttribute(9, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseLeave));
            builder.AddAttribute(10, "onpointerdown", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.PointerEventArgs>(this, e => OnPointerDown(e, index)));
            builder.AddEventStopPropagationAttribute(11, "onpointerdown", CreatesVertices);
            builder.CloseElement();
        };
    }

    private void OnPointerDown(PointerEventArgs e, int index)
    {
        if (!CreatesVertices)
            return;

        var vertex = CreateVertex(e.ClientX, e.ClientY, index);
        BlazorDiagram.TriggerPointerDown(vertex, e.ToCore());
    }

    private void OnMouseEnter(MouseEventArgs e)
    {
        _hovered = true;
    }

    private void OnMouseLeave(MouseEventArgs e)
    {
        _hovered = false;
    }

    private LinkVertexModel CreateVertex(double clientX, double clientY, int index)
    {
        var rPt = BlazorDiagram.GetRelativeMousePoint(clientX, clientY);
        return Link.InsertVertex(index, rPt);
    }
}
