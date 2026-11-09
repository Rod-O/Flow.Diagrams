using System;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;

namespace Flow.Diagrams.Components.Widgets;

/// <summary>
/// Rubber-band selection: holding Shift and dragging on empty canvas draws a rectangle and selects every node it overlaps,
/// deselecting those it no longer overlaps. Groups and links are not selected. Put it in <see cref="DiagramCanvas.Widgets"/>.
/// </summary>
public partial class SelectionBoxWidget : IDisposable
{
    private Point? _initialClientPoint;
    private Size? _selectionBoxSize; // Todo: remove unneeded instantiations
    private Point? _selectionBoxTopLeft; // Todo: remove unneeded instantiations

    /// <summary>The diagram this component belongs to; supplied as a cascading value.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;

    /// <summary>CSS background of the rectangle.</summary>
    [Parameter] public string Background { get; set; } = "rgb(110 159 212 / 25%)";

    /// <inheritdoc />
    public void Dispose()
    {
        BlazorDiagram.PointerDown -= OnPointerDown;
        BlazorDiagram.PointerMove -= OnPointerMove;
        BlazorDiagram.PointerUp -= OnPointerUp;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        BlazorDiagram.PointerDown += OnPointerDown;
        BlazorDiagram.PointerMove += OnPointerMove;
        BlazorDiagram.PointerUp += OnPointerUp;
    }

    private string GenerateStyle()
    {
        return FormattableString.Invariant(
            $"position: absolute; background: {Background}; top: {_selectionBoxTopLeft!.Y}px; left: {_selectionBoxTopLeft.X}px; width: {_selectionBoxSize!.Width}px; height: {_selectionBoxSize.Height}px;");
    }

    private void OnPointerDown(Model? model, MouseEventArgs e)
    {
        if (model != null || !e.ShiftKey)
            return;

        _initialClientPoint = new Point(e.ClientX, e.ClientY);
    }

    private void OnPointerMove(Model? model, MouseEventArgs e)
    {
        if (_initialClientPoint == null)
            return;

        SetSelectionBoxInformation(e);

        var start = BlazorDiagram.GetRelativeMousePoint(_initialClientPoint.X, _initialClientPoint.Y);
        var end = BlazorDiagram.GetRelativeMousePoint(e.ClientX, e.ClientY);
        var (sX, sY) = (Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
        var (eX, eY) = (Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
        var bounds = new Rectangle(sX, sY, eX, eY);

        foreach (var node in BlazorDiagram.Nodes)
        {
            var nodeBounds = node.GetBounds();
            if (nodeBounds == null)
                continue;

            if (bounds.Intersects(nodeBounds))
                BlazorDiagram.SelectModel(node, false);
            else if (node.Selected) BlazorDiagram.UnselectModel(node);
        }

        InvokeAsync(StateHasChanged);
    }

    private void SetSelectionBoxInformation(MouseEventArgs e)
    {
        var start = BlazorDiagram.GetRelativePoint(_initialClientPoint!.X, _initialClientPoint.Y);
        var end = BlazorDiagram.GetRelativePoint(e.ClientX, e.ClientY);
        var (sX, sY) = (Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
        var (eX, eY) = (Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
        _selectionBoxTopLeft = new Point(sX, sY);
        _selectionBoxSize = new Size(eX - sX, eY - sY);
    }

    private void OnPointerUp(Model? model, MouseEventArgs e)
    {
        _initialClientPoint = null;
        _selectionBoxTopLeft = null;
        _selectionBoxSize = null;
        InvokeAsync(StateHasChanged);
    }
}