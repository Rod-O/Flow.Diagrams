using System;
using System.Text;
using Flow.Diagrams.Core.Extensions;
using Microsoft.AspNetCore.Components;

namespace Flow.Diagrams.Components.Widgets;

/// <summary>
/// A background grid that pans and zooms with the diagram. Put it in <see cref="DiagramCanvas.Widgets"/>; it draws a
/// CSS background, so it is not part of an SVG export.
/// </summary>
public partial class GridWidget : IDisposable
{
    private bool _visible;
    private double _scaledSize;
    private double _posX;
    private double _posY;
    
    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;
    /// <summary>Distance between grid lines or points, in diagram units; drawn <c>Size * Zoom</c> pixels apart.</summary>
    [Parameter] public double Size { get; set; } = 20;
    /// <summary>The grid is hidden when the diagram's zoom is at or below this value, which keeps a dense grid from filling the screen when zoomed out; the default 0 never hides it.</summary>
    [Parameter] public double ZoomThreshold { get; set; } = 0;
    /// <summary>Whether the grid is drawn as lines or as points.</summary>
    [Parameter] public GridMode Mode { get; set; } = GridMode.Line;
    /// <summary>CSS color behind the grid.</summary>
    [Parameter] public string BackgroundColor { get; set; } = "rgb(241 241 241)";

    /// <inheritdoc />
    public void Dispose()
    {
        BlazorDiagram.PanChanged -= RefreshPosition;
        BlazorDiagram.ZoomChanged -= RefreshPosition;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        BlazorDiagram.PanChanged += RefreshPosition;
        BlazorDiagram.ZoomChanged += RefreshPosition;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _posX = BlazorDiagram.Pan.X;
        _posY = BlazorDiagram.Pan.Y;
        _scaledSize = Size * BlazorDiagram.Zoom;
        _visible = BlazorDiagram.Zoom > ZoomThreshold;
    }

    private void RefreshPosition()
    {
        _posX = BlazorDiagram.Pan.X;
        _posY = BlazorDiagram.Pan.Y;
        _scaledSize = Size * BlazorDiagram.Zoom;
        _visible = BlazorDiagram.Zoom > ZoomThreshold;
        InvokeAsync(StateHasChanged);
    }

    private string GenerateStyle()
    {
        var sb = new StringBuilder();

        sb.Append($"background-color: {BackgroundColor};");
        sb.Append($"background-size: {_scaledSize.ToInvariantString()}px {_scaledSize.ToInvariantString()}px;");
        sb.Append($"background-position-x: {_posX.ToInvariantString()}px;");
        sb.Append($"background-position-y: {_posY.ToInvariantString()}px;");

        switch (Mode)
        {
            case GridMode.Line:
                sb.Append("background-image: linear-gradient(rgb(211, 211, 211) 1px, transparent 1px), linear-gradient(90deg, rgb(211, 211, 211) 1px, transparent 1px);");
                break;
            case GridMode.Point:
                sb.Append("background-image: radial-gradient(circle at 0 0, rgb(129, 129, 129) 1px, transparent 1px);");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return sb.ToString();
    }
}

/// <summary>How <see cref="GridWidget"/> draws its grid.</summary>
public enum GridMode
{
    /// <summary>A grid of horizontal and vertical lines.</summary>
    Line,
    /// <summary>A grid of dots at the line intersections.</summary>
    Point
}