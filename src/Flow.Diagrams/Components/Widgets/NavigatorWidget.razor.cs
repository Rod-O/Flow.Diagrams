using System;
using System.Linq;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Flow.Diagrams.Components.Widgets;

/// <summary>
/// A miniature of the whole diagram with a rectangle marking the part currently on screen. It is sized to fit every node and
/// group plus the current view, and refreshes when the diagram, a node or a group changes. Set <see cref="Width"/> and
/// <see cref="Height"/> to size it; a non-positive value leaves the miniature empty.
/// </summary>
public partial class NavigatorWidget : IDisposable
{
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private double _scaledMargin;
    private double _vX;
    private double _vY;
    private double _vWidth;
    private double _vHeight;

    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;
    /// <summary>Whether nodes whose shape is an ellipse are drawn as ellipses; otherwise, and for every other shape, as their bounding rectangle.</summary>
    [Parameter] public bool UseNodeShape { get; set; } = true;
    /// <summary>Width of the widget, in screen pixels; 200 by default. The miniature stays empty while it is not positive.</summary>
    [Parameter] public double Width { get; set; } = 200;
    /// <summary>Height of the widget, in screen pixels; 150 by default. The miniature stays empty while it is not positive.</summary>
    [Parameter] public double Height { get; set; } = 150;
    /// <summary>Space kept around the drawing inside the widget, in screen pixels.</summary>
    [Parameter] public double Margin { get; set; } = 5;
    /// <summary>CSS fill of nodes in the miniature.</summary>
    [Parameter] public string NodeColor { get; set; } = "#40babd";
    /// <summary>CSS fill of groups in the miniature.</summary>
    [Parameter] public string GroupColor { get; set; } = "#9fd0d1";
    /// <summary>CSS stroke color of the rectangle marking the visible area.</summary>
    [Parameter] public string ViewStrokeColor { get; set; } = "#40babd";
    /// <summary>Stroke width of the visible-area rectangle, in diagram units, not screen pixels.</summary>
    [Parameter] public int ViewStrokeWidth { get; set; } = 4;
    /// <summary>Extra CSS classes for the root <c>svg</c> element, added after <c>navigator</c>; <c>default</c> gives it the look and corner placement of <c>default.styles.css</c>.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Inline CSS for the root <c>svg</c> element, for positioning the widget over the canvas.</summary>
    [Parameter] public string? Style { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        BlazorDiagram.Changed -= Refresh;
        BlazorDiagram.Nodes.Added -= OnNodeAdded;
        BlazorDiagram.Nodes.Removed -= OnNodeRemoved;
        BlazorDiagram.Groups.Added -= OnNodeAdded;
        BlazorDiagram.Groups.Removed -= OnNodeRemoved;

        foreach (var node in BlazorDiagram.Nodes)
            node.Changed -= OnNodeChanged;

        foreach (var group in BlazorDiagram.Groups)
            group.Changed -= OnNodeChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        BlazorDiagram.Changed += Refresh;
        BlazorDiagram.Nodes.Added += OnNodeAdded;
        BlazorDiagram.Nodes.Removed += OnNodeRemoved;
        BlazorDiagram.Groups.Added += OnNodeAdded;
        BlazorDiagram.Groups.Removed += OnNodeRemoved;

        foreach (var node in BlazorDiagram.Nodes)
            node.Changed += OnNodeChanged;

        foreach (var group in BlazorDiagram.Groups)
            group.Changed += OnNodeChanged;
    }

    private void OnNodeAdded(NodeModel node) => node.Changed += OnNodeChanged;

    private void OnNodeRemoved(NodeModel node) => node.Changed -= OnNodeChanged;

    private void OnNodeChanged(Model _) => Refresh();

    private void Refresh()
    {
        if (BlazorDiagram.Container == null || Width <= 0 || Height <= 0)
            return;

        _vX = -BlazorDiagram.Pan.X / BlazorDiagram.Zoom;
        _vY = -BlazorDiagram.Pan.Y / BlazorDiagram.Zoom;
        _vWidth = BlazorDiagram.Container.Width / BlazorDiagram.Zoom;
        _vHeight = BlazorDiagram.Container.Height / BlazorDiagram.Zoom;

        var minX = _vX;
        var minY = _vY;
        var maxX = _vX + _vWidth;
        var maxY = _vY + _vHeight;

        foreach (var node in BlazorDiagram.Nodes.Union(BlazorDiagram.Groups))
        {
            if (node.Size == null)
                continue;

            minX = Math.Min(minX, node.Position.X);
            minY = Math.Min(minY, node.Position.Y);
            maxX = Math.Max(maxX, node.Position.X + node.Size.Width);
            maxY = Math.Max(maxY, node.Position.Y + node.Size.Height);
        }

        var width = maxX - minX;
        var height = maxY - minY;
        var scaledWidth = width / Width;
        var scaledHeight = height / Height;
        var scale = Math.Max(scaledWidth, scaledHeight);
        var viewWidth = scale * Width;
        var viewHeight = scale * Height;

        _scaledMargin = Margin * scale;
        _x = minX - (viewWidth - width) / 2 - _scaledMargin;
        _y = minY - (viewHeight - height) / 2 - _scaledMargin;
        _width = viewWidth + _scaledMargin * 2;
        _height = viewHeight + _scaledMargin * 2;
        InvokeAsync(StateHasChanged);
    }

    private RenderFragment GetNodeRenderFragment(NodeModel node)
    {
        return builder =>
        {
            if (UseNodeShape)
            {
                var shape = node.GetShape();
                if (shape is Ellipse ellipse)
                {
                    RenderEllipse(node, builder, ellipse);
                    return;
                }
            }
            
            RenderRect(node, builder);
        };
    }

    private void RenderRect(NodeModel node, RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "rect");
        builder.SetKey(node);
        builder.AddAttribute(1, "class", "navigator-node");
        builder.AddAttribute(2, "fill", NodeColor);
        builder.AddAttribute(3, "x", node.Position.X.ToInvariantString());
        builder.AddAttribute(4, "y", node.Position.Y.ToInvariantString());
        builder.AddAttribute(5, "width", node.Size!.Width.ToInvariantString());
        builder.AddAttribute(6, "height", node.Size.Height.ToInvariantString());
        builder.CloseElement();
    }

    private void RenderEllipse(NodeModel node, RenderTreeBuilder builder, Ellipse ellipse)
    {
        builder.OpenElement(0, "ellipse");
        builder.SetKey(node);
        builder.AddAttribute(1, "class", "navigator-node");
        builder.AddAttribute(2, "fill", NodeColor);
        builder.AddAttribute(3, "cx", ellipse.Cx.ToInvariantString());
        builder.AddAttribute(4, "cy", ellipse.Cy.ToInvariantString());
        builder.AddAttribute(5, "rx",ellipse.Rx.ToInvariantString());
        builder.AddAttribute(6, "ry", ellipse.Ry.ToInvariantString());
        builder.CloseElement();
    }
}