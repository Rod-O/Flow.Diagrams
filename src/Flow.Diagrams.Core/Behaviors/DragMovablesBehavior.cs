using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Events;
using System;
using System.Collections.Generic;
using Flow.Diagrams.Core.Models;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Moves the selected nodes, groups and other movable models while the pointer is dragged, snapping to
/// <see cref="Options.DiagramOptions.GridSize"/> when set. Locked models and nodes of a group without auto size stay put. Registered by default.
/// <see cref="Models.Base.MovableModel.TriggerMoved"/> is called on each moved model when the pointer is released after a real drag.
/// </summary>
public class DragMovablesBehavior : Behavior
{
    private readonly Dictionary<MovableModel, Point> _initialPositions;
    private double? _lastClientX;
    private double? _lastClientY;
    private bool _moved;

    /// <summary>Starts listening to pointer events of <paramref name="diagram"/>.</summary>
    public DragMovablesBehavior(Diagram diagram) : base(diagram)
    {
        _initialPositions = new Dictionary<MovableModel, Point>();
        Diagram.PointerDown += OnPointerDown;
        Diagram.PointerMove += OnPointerMove;
        Diagram.PointerUp += OnPointerUp;
    }

    private void OnPointerDown(Model? model, PointerEventArgs e)
    {
        if (model is not MovableModel)
            return;

        _initialPositions.Clear();
        foreach (var sm in Diagram.GetSelectedModels())
        {
            if (sm is not MovableModel movable || movable.Locked)
                continue;

            if (sm is NodeModel node && node.Group != null && !node.Group.AllowChildDrag)
                continue;

            var position = movable.Position;
            if (Diagram.Options.GridSnapToCenter && movable is NodeModel n)
            {
                position = new Point(movable.Position.X + (n.Size?.Width ?? 0) / 2,
                    movable.Position.Y + (n.Size?.Height ?? 0) / 2);
            }

            _initialPositions.Add(movable, position);
        }

        _lastClientX = e.ClientX;
        _lastClientY = e.ClientY;
        _moved = false;
    }

    private void OnPointerMove(Model? model, PointerEventArgs e)
    {
        if (_initialPositions.Count == 0 || _lastClientX == null || _lastClientY == null)
            return;

        _moved = true;
        var deltaX = (e.ClientX - _lastClientX.Value) / Diagram.Zoom;
        var deltaY = (e.ClientY - _lastClientY.Value) / Diagram.Zoom;

        foreach (var (movable, initialPosition) in _initialPositions)
        {
            var ndx = ApplyGridSize(deltaX + initialPosition.X);
            var ndy = ApplyGridSize(deltaY + initialPosition.Y);
            if (Diagram.Options.GridSnapToCenter && movable is NodeModel node)
            {
                node.SetPosition(ndx - (node.Size?.Width ?? 0) / 2, ndy - (node.Size?.Height ?? 0) / 2);
            }
            else
            {
                movable.SetPosition(ndx, ndy);
            }
        }
    }

    private void OnPointerUp(Model? model, PointerEventArgs e)
    {
        if (_initialPositions.Count == 0)
            return;

        if (_moved)
        {
            foreach (var (movable, _) in _initialPositions)
            {
                movable.TriggerMoved();
            }
        }
        
        _initialPositions.Clear();
        _lastClientX = null;
        _lastClientY = null;
    }

    private double ApplyGridSize(double n)
    {
        if (Diagram.Options.GridSize == null)
            return n;

        var gridSize = Diagram.Options.GridSize.Value;
        return gridSize * Math.Floor((n + gridSize / 2.0) / gridSize);
    }

    /// <summary>Stops listening to pointer events.</summary>
    public override void Dispose()
    {
        _initialPositions.Clear();
        
        Diagram.PointerDown -= OnPointerDown;
        Diagram.PointerMove -= OnPointerMove;
        Diagram.PointerUp -= OnPointerUp;
    }
}
