using System;
using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Models.Base;

// I'm assuming that all movable models (nodes & groups for now) are also selectable,
// I believe it makes sense since if you click to move something then you're also selecting
/// <summary>A selectable model with a position that the user can drag (nodes, groups and link vertices).</summary>
public abstract class MovableModel : SelectableModel
{
    /// <summary>
    /// Raised once when a drag that moved the model ends, by <see cref="TriggerMoved"/>. Not raised by <see
    /// cref="SetPosition"/>, so it is not fired for programmatic moves or while the drag is in progress.
    /// </summary>
    public event Action<MovableModel>? Moved;
    
    /// <summary>Creates a model at <paramref name="position"/>, or at the origin when <c>null</c>.</summary>
    public MovableModel(Point? position = null)
    {
        Position = position ?? Point.Zero;
    }

    /// <summary>
    /// Creates a model with the given <paramref name="id"/> at <paramref name="position"/>, or at the origin when
    /// <c>null</c>.
    /// </summary>
    public MovableModel(string id, Point? position = null) : base(id)
    {
        Position = position ?? Point.Zero;
    }

    /// <summary>
    /// Top-left corner in diagram units. Assigning it only stores the value; use <see cref="SetPosition"/> to move
    /// the model together with what depends on it (ports, links, children).
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// Moves the model to (<paramref name="x"/>, <paramref name="y"/>) in diagram units. The base version only
    /// assigns <see cref="Position"/>; overrides also move and refresh dependents.
    /// </summary>
    public virtual void SetPosition(double x, double y) => Position = new Point(x, y);

    /// <summary>
    /// Only use this if you know what you're doing
    /// </summary>
    public void TriggerMoved() => Moved?.Invoke(this);
}
