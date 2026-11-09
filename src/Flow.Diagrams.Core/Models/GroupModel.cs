using System;
﻿using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Diagrams.Core.Models;

/// <summary>
/// A node that wraps other nodes: it positions and sizes itself around its <see cref="Children"/> plus padding, and
/// moving the group moves them. Groups live in <see cref="Diagram.Groups"/>, not <see cref="Diagram.Nodes"/>;
/// children stay in <see cref="Diagram.Nodes"/>.
/// </summary>
public class GroupModel : NodeModel
{
    private readonly List<NodeModel> _children;

    /// <summary>
    /// Creates a group around <paramref name="children"/> with the same <paramref name="padding"/> in diagram units
    /// on every side. <paramref name="allowChildDrag"/> sets <see cref="AllowChildDrag"/>.
    /// </summary>
    public GroupModel(IEnumerable<NodeModel> children, byte padding = 30, bool allowChildDrag = true)
        : this(children, new GroupPadding(padding), allowChildDrag)
    {
    }

    /// <summary>
    /// Creates a group around <paramref name="children"/> with the given padding per side. <paramref
    /// name="allowChildDrag"/> sets <see cref="AllowChildDrag"/>.
    /// </summary>
    public GroupModel(IEnumerable<NodeModel> children, GroupPadding padding, bool allowChildDrag = true)
    {
        _children = new List<NodeModel>();

        Size = Size.Zero;
        PaddingBySide = padding;
        Padding = (byte)Math.Clamp(Math.Round(Math.Min(Math.Min(padding.Left, padding.Top), Math.Min(padding.Right, padding.Bottom))), 0, byte.MaxValue);
        AllowChildDrag = allowChildDrag;
        Initialize(children);
    }

    /// <summary>The nodes the group wraps, including nested groups.</summary>
    public IReadOnlyList<NodeModel> Children => _children;

    /// <summary>
    /// The uniform padding given to the constructor. When the group was created with a <see cref="GroupPadding"/>,
    /// this is its smallest side, rounded; <see cref="PaddingBySide"/> is what sizes the group.
    /// </summary>
    public byte Padding { get; }

    /// <summary>Space between the children's bounds and the group's edges, per side.</summary>
    public GroupPadding PaddingBySide { get; }
    /// <summary>
    /// When <c>true</c> (the default), children can be dragged individually; when <c>false</c>, a selected child is
    /// not moved on its own by dragging and the group moves only by its frame. The group sizes to its children
    /// either way.
    /// </summary>
    public bool AllowChildDrag { get; }

    /// <summary>
    /// Adds <paramref name="child"/>, sets its <see cref="NodeModel.Group"/>, and resizes the group when the children
    /// can be measured.
    /// </summary>
    public void AddChild(NodeModel child)
    {
        _children.Add(child);
        child.Group = this;
        child.SizeChanged += OnNodeChanged;
        child.Moving += OnNodeChanged;

        if (UpdateDimensions())
        {
            Refresh();
        }
    }

    /// <summary>Removes <paramref name="child"/> and resizes the group; does nothing if it is not a child.</summary>
    public void RemoveChild(NodeModel child)
    {
        if (!_children.Remove(child))
            return;

        child.Group = null;
        child.SizeChanged -= OnNodeChanged;
        child.Moving -= OnNodeChanged;

        if (UpdateDimensions())
        {
            Refresh();
            RefreshLinks();
        }
    }

    /// <summary>Moves the group and every child by the same offset, then refreshes them and their links.</summary>
    public override void SetPosition(double x, double y)
    {
        var deltaX = x - Position.X;
        var deltaY = y - Position.Y;
        base.SetPosition(x, y);

        foreach (var node in Children)
        {
            node.UpdatePositionSilently(deltaX, deltaY);
            node.RefreshLinks();
        }

        Refresh();
        RefreshLinks();
    }

    /// <summary>Moves the group and every child by the given offset without refreshing links.</summary>
    public override void UpdatePositionSilently(double deltaX, double deltaY)
    {
        base.UpdatePositionSilently(deltaX, deltaY);

        foreach (var child in Children)
            child.UpdatePositionSilently(deltaX, deltaY);

        Refresh();
    }

    /// <summary>
    /// Releases every child (they stay in the diagram) and empties the group; the group itself is not removed from
    /// <see cref="Diagram.Groups"/>.
    /// </summary>
    public void Ungroup()
    {
        foreach (var child in Children)
        {
            child.Group = null;
            child.SizeChanged -= OnNodeChanged;
            child.Moving -= OnNodeChanged;
        }

        _children.Clear();
    }

    private void Initialize(IEnumerable<NodeModel> children)
    {
        foreach (var child in children)
        {
            _children.Add(child);
            child.Group = this; 
            child.SizeChanged += OnNodeChanged;
            child.Moving += OnNodeChanged;
        }

        UpdateDimensions();
    }

    private void OnNodeChanged(NodeModel node)
    {
        if (UpdateDimensions())
        {
            Refresh();
        }
    }

    /// <summary>
    /// Recomputes the group's position and size from <see cref="GetChildrenBounds"/> and the padding. Returns
    /// <c>false</c> when the children cannot be measured yet, in which case nothing changed.
    /// </summary>
    protected virtual bool UpdateDimensions()
    {
        if (Children.Count == 0)
            return true;

        if (GetChildrenBounds() is not { } bounds)
            return false;

        var padding = PaddingBySide;

        var newPosition = new Point(bounds.Left - padding.Left, bounds.Top - padding.Top);
        if (!Position.Equals(newPosition))
        {
            // NodeModel's version moves the ports with the box; the group's own override would also move the children.
            base.UpdatePositionSilently(newPosition.X - Position.X, newPosition.Y - Position.Y);
            TriggerMoving();
        }

        Size = new Size(bounds.Width + padding.Left + padding.Right, bounds.Height + padding.Top + padding.Bottom);
        return true;
    }

    /// <summary>
    /// The area the group wraps, before padding, or null while it cannot be measured yet. By default the union
    /// of the children's <see cref="Flow.Diagrams.Core.Models.Base.MovableModel.Position"/> and <see cref="NodeModel.Size"/>, available once every
    /// child has a size. Override it when a child's visual extent differs from that rectangle (for example an SVG
    /// symbol drawn around its origin).
    /// </summary>
    protected virtual Rectangle? GetChildrenBounds() => Children.Any(n => n.Size == null) ? null : Children.GetBounds();
}
