using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.ChangeTracking;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Layers;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Events;
using System.Runtime.CompilerServices;
using Flow.Diagrams.Core.Options;
using Flow.Diagrams.Core.Controls;

[assembly: InternalsVisibleTo("Flow.Diagrams")]
[assembly: InternalsVisibleTo("Flow.Diagrams.Tests")]
[assembly: InternalsVisibleTo("Flow.Diagrams.Core.Tests")]

namespace Flow.Diagrams.Core;

/// <summary>
/// The model of one diagram: its <see cref="Nodes"/>, <see cref="Links"/> and <see cref="Groups"/>, the pan and zoom
/// of its viewport, the registered <see cref="Behavior"/>s, and the pointer, keyboard and wheel events that drive
/// them. It renders nothing itself; the Blazor subclass <c>BlazorDiagram</c> in the Flow.Diagrams package does.
/// Pan and zoom are in screen pixels and a unitless scale; positions of models are in diagram units. Changes call
/// <see cref="Refresh"/>, which raises <see cref="Changed"/> so the renderer redraws.
/// </summary>
public abstract class Diagram
{
    private readonly Dictionary<Type, Behavior> _behaviors;
    private readonly List<SelectableModel> _orderedSelectables;

    /// <summary>
    /// Raised when a pointer button goes down; the argument is the model under the pointer, or <c>null</c> on the
    /// empty canvas. Raised by the renderer through <see cref="TriggerPointerDown"/>.
    /// </summary>
    public event Action<Model?, PointerEventArgs>? PointerDown;
    /// <summary>
    /// Raised when the pointer moves over the canvas; the model argument is always <c>null</c> when raised by the
    /// canvas component.
    /// </summary>
    public event Action<Model?, PointerEventArgs>? PointerMove;
    /// <summary>
    /// Raised when a pointer button goes up; the argument is the model under the pointer, or <c>null</c> on the empty
    /// canvas.
    /// </summary>
    public event Action<Model?, PointerEventArgs>? PointerUp;
    /// <summary>Raised when the pointer enters a node, group or link; the argument is that model.</summary>
    public event Action<Model?, PointerEventArgs>? PointerEnter;
    /// <summary>Raised when the pointer leaves a node, group or link; the argument is that model.</summary>
    public event Action<Model?, PointerEventArgs>? PointerLeave;
    /// <summary>Raised when a key is pressed while the canvas has focus.</summary>
    public event Action<KeyboardEventArgs>? KeyDown;
    /// <summary>
    /// Raised when the mouse wheel turns over the canvas; <see cref="Behaviors.ZoomBehavior"/> uses it to zoom.
    /// </summary>
    public event Action<WheelEventArgs>? Wheel;
    /// <summary>
    /// Raised on pointer up when the pointer did not move since the matching pointer down on the same model. Raised
    /// only while <see cref="Behaviors.EventsBehavior"/> is registered.
    /// </summary>
    public event Action<Model?, PointerEventArgs>? PointerClick;
    /// <summary>
    /// Raised right after a <see cref="PointerClick"/> that follows the previous click within 500 ms. Raised only
    /// while <see cref="Behaviors.EventsBehavior"/> is registered.
    /// </summary>
    public event Action<Model?, PointerEventArgs>? PointerDoubleClick;

    /// <summary>
    /// Raised once for each model that becomes selected or unselected; <see cref="UnselectAll"/> therefore raises it
    /// once per selected model. The argument is that model, whose <see cref="SelectableModel.Selected"/> is already
    /// updated.
    /// </summary>
    public event Action<SelectableModel>? SelectionChanged;
    /// <summary>Raised after <see cref="Pan"/> changes.</summary>
    public event Action? PanChanged;
    /// <summary>Raised after <see cref="Zoom"/> changes.</summary>
    public event Action? ZoomChanged;
    /// <summary>Raised after <see cref="Container"/> is set to a different rectangle.</summary>
    public event Action? ContainerChanged;
    /// <summary>
    /// Raised by <see cref="Refresh"/> whenever something in the diagram changed and it should be redrawn. Not raised
    /// while <see cref="SuspendRefresh"/> is <c>true</c>.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Creates the layers and, unless <paramref name="registerDefaultBehaviors"/> is <c>false</c>, registers the
    /// default behaviors (selection, dragging, link creation, pan, zoom, events, keyboard shortcuts, controls and
    /// virtualization).
    /// </summary>
    protected Diagram(bool registerDefaultBehaviors = true)
    {
        _behaviors = new Dictionary<Type, Behavior>();
        _orderedSelectables = new List<SelectableModel>();

        Nodes = new NodeLayer(this);
        Links = new LinkLayer(this);
        Groups = new GroupLayer(this);
        Controls = new ControlsLayer();

        Nodes.Added += OnSelectableAdded;
        Links.Added += OnSelectableAdded;
        Groups.Added += OnSelectableAdded;

        Nodes.Removed += OnSelectableRemoved;
        Links.Removed += OnSelectableRemoved;
        Groups.Removed += OnSelectableRemoved;

        if (!registerDefaultBehaviors)
            return;

        RegisterBehavior(new SelectionBehavior(this));
        RegisterBehavior(new DragMovablesBehavior(this));
        RegisterBehavior(new DragNewLinkBehavior(this));
        RegisterBehavior(new PanBehavior(this));
        RegisterBehavior(new ZoomBehavior(this));
        RegisterBehavior(new EventsBehavior(this));
        RegisterBehavior(new KeyboardShortcutsBehavior(this));
        RegisterBehavior(new ControlsBehavior(this));
        RegisterBehavior(new VirtualizationBehavior(this));
    }

    /// <summary>
    /// The options that configure this diagram's behaviors; read each time they apply, so changes take effect
    /// immediately.
    /// </summary>
    public abstract DiagramOptions Options { get; }
    /// <summary>The nodes of the diagram, excluding groups.</summary>
    public NodeLayer Nodes { get; }
    /// <summary>The links of the diagram.</summary>
    public LinkLayer Links { get; }
    /// <summary>The groups of the diagram; a group is not also in <see cref="Nodes"/>.</summary>
    public GroupLayer Groups { get; }
    /// <summary>The controls (such as the remove button) attached to models.</summary>
    public ControlsLayer Controls { get; }
    /// <summary>
    /// The canvas rectangle in client (screen) pixels, or <c>null</c> until the renderer has measured it and called
    /// <see cref="SetContainer"/>. Coordinate conversions and zoom-to-fit need it.
    /// </summary>
    public Rectangle? Container { get; private set; }
    /// <summary>
    /// The translation of the viewport in screen pixels; change it with <see cref="SetPan"/> or <see
    /// cref="UpdatePan"/>.
    /// </summary>
    public Point Pan { get; private set; } = Point.Zero;
    /// <summary>The scale of the viewport, 1 by default; change it with <see cref="SetZoom"/>.</summary>
    public double Zoom { get; private set; } = 1;
    /// <summary>
    /// While <c>true</c>, <see cref="Refresh"/> does nothing. Prefer <see cref="Batch"/>, which restores it for you.
    /// </summary>
    public bool SuspendRefresh { get; set; }
    /// <summary>Collects model changes so each model is raised once per <see cref="Batch"/>.</summary>
    internal ChangeTracker Changes { get; } = new();
    /// <summary>
    /// While <c>true</c>, changing <see cref="SelectableModel.Order"/> or <see cref="SelectableModel.Layer"/> does
    /// not re-sort <see cref="OrderedSelectables"/>. The ordering methods set it internally and always restore it;
    /// leave it <c>false</c>.
    /// </summary>
    public bool SuspendSorting { get; set; }
    /// <summary>
    /// Every node, link and group in drawing order (by <see cref="SelectableModel.Layer"/>, then <see
    /// cref="SelectableModel.Order"/>), back to front. This is a live view; link vertices are not in it.
    /// </summary>
    public IReadOnlyList<SelectableModel> OrderedSelectables => _orderedSelectables;

    /// <summary>
    /// Raises <see cref="Changed"/> so the diagram is redrawn, unless <see cref="SuspendRefresh"/> is <c>true</c>.
    /// </summary>
    public void Refresh()
    {
        if (SuspendRefresh)
            return;

        Changed?.Invoke();
    }

    /// <summary>
    /// Runs <paramref name="action"/> with refreshes suspended and model changes deferred, then raises each changed
    /// model's <see cref="Model.Changed"/> once and calls <see cref="Refresh"/> once. Inside an outer batch (or any
    /// suspended refresh) it only runs the action and leaves both to the outer caller. If the action throws, the
    /// models are still raised, refreshes restored and the diagram refreshed (whatever the action changed before
    /// throwing is drawn), then the action's exception propagates; an exception from a model's handler propagates
    /// only when the action did not throw.
    /// </summary>
    public void Batch(Action action)
    {
        if (SuspendRefresh)
        {
            // If it's already suspended, just execute the action and leave it suspended
            // It's probably handled by an outer batch
            action();
            return;
        }

        SuspendRefresh = true;
        var suspension = Changes.Suspend();
        try
        {
            try
            {
                action();
            }
            catch
            {
                try
                {
                    suspension.Dispose();
                }
                catch (Exception)
                {
                    // The action's exception is the one the caller sees.
                }

                throw;
            }

            suspension.Dispose();
        }
        finally
        {
            SuspendRefresh = false;
            Refresh();
        }
    }

    #region Selection

    /// <summary>
    /// Lazily enumerates the selected nodes, then for each link the link itself and its selected vertices, then the
    /// selected groups.
    /// </summary>
    public IEnumerable<SelectableModel> GetSelectedModels()
    {
        foreach (var node in Nodes)
        {
            if (node.Selected)
                yield return node;
        }

        foreach (var link in Links)
        {
            if (link.Selected)
                yield return link;

            foreach (var vertex in link.Vertices)
            {
                if (vertex.Selected)
                    yield return vertex;
            }
        }

        foreach (var group in Groups)
        {
            if (group.Selected)
                yield return group;
        }
    }

    /// <summary>
    /// Selects <paramref name="model"/>, redraws it and raises <see cref="SelectionChanged"/>. With <paramref
    /// name="unselectOthers"/>, everything else is unselected first. If it is already selected, it is left as is
    /// (no refresh, no event for it); only the others are unselected.
    /// </summary>
    public void SelectModel(SelectableModel model, bool unselectOthers)
    {
        if (unselectOthers)
        {
            foreach (var other in GetSelectedModels().ToList())
            {
                if (other != model)
                    UnselectModel(other);
            }
        }

        if (model.Selected)
            return;

        model.Selected = true;
        SelectionChanged?.Invoke(model);
    }

    /// <summary>
    /// Unselects <paramref name="model"/>, redraws it and raises <see cref="SelectionChanged"/>; does nothing if it
    /// is not selected.
    /// </summary>
    public void UnselectModel(SelectableModel model)
    {
        if (!model.Selected)
            return;

        model.Selected = false;
        SelectionChanged?.Invoke(model);
    }

    /// <summary>
    /// Unselects every selected model, redrawing each and raising <see cref="SelectionChanged"/> once per model.
    /// </summary>
    public void UnselectAll()
    {
        foreach (var model in GetSelectedModels())
        {
            model.Selected = false;
            // Todo: will result in many events, maybe one event for all of them?
            SelectionChanged?.Invoke(model);
        }
    }

    #endregion

    #region Behaviors

    /// <summary>
    /// Adds <paramref name="behavior"/>, keyed by its exact type. Throws <see cref="Exception"/> if a behavior of
    /// that type is already registered.
    /// </summary>
    public void RegisterBehavior(Behavior behavior)
    {
        var type = behavior.GetType();
        if (_behaviors.ContainsKey(type))
            throw new DiagramsException($"Behavior '{type.Name}' already registered");

        _behaviors.Add(type, behavior);
    }

    /// <summary>
    /// Returns the registered behavior whose exact type is <typeparamref name="T"/>; when there is none, a registered
    /// behavior that derives from <typeparamref name="T"/> (if several do, which one is unspecified); otherwise <c>null</c>.
    /// </summary>
    public T? GetBehavior<T>() where T : Behavior
    {
        if (_behaviors.TryGetValue(typeof(T), out var exact))
            return (T)exact;

        return _behaviors.Values.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Disposes and removes the behavior that <see cref="GetBehavior{T}"/> returns: the one whose exact type is
    /// <typeparamref name="T"/>, else a registered behavior that derives from <typeparamref name="T"/>. Does nothing
    /// if there is none.
    /// </summary>
    public void UnregisterBehavior<T>() where T : Behavior
    {
        var behavior = GetBehavior<T>();
        if (behavior == null)
            return;

        behavior.Dispose();
        _behaviors.Remove(behavior.GetType());
    }

    #endregion

    /// <summary>
    /// Sets zoom and pan so the selected nodes fit in <see cref="Container"/>, or all nodes when none is selected,
    /// with <paramref name="margin"/> diagram units around them. Only nodes count (not links) and the zoom is clamped
    /// to <see cref="DiagramZoomOptions"/>. Does nothing without nodes, or without a container or while it has no width
    /// or height (a canvas in a hidden tab). Zoom and pan are applied in one <see cref="Batch"/>, so refreshes are
    /// restored even if a handler throws.
    /// </summary>
    public void ZoomToFit(double margin = 10)
    {
        if (Container == null || Container.Width <= 0 || Container.Height <= 0 || Nodes.Count == 0)
            return;

        var selectedNodes = Nodes.Where(s => s.Selected);
        var nodesToUse = selectedNodes.Any() ? selectedNodes : Nodes;
        var bounds = nodesToUse.GetBounds();
        var width = bounds.Width + 2 * margin;
        var height = bounds.Height + 2 * margin;
        var minX = bounds.Left - margin;
        var minY = bounds.Top - margin;

        var container = Container;
        Batch(() =>
        {
            SetZoom(Math.Min(container.Width / width, container.Height / height));

            var nx = container.Left + Pan.X + minX * Zoom;
            var ny = container.Top + Pan.Y + minY * Zoom;
            UpdatePan(container.Left - nx, container.Top - ny);
        });
    }

    /// <summary>
    /// Sets <see cref="Pan"/> to (<paramref name="x"/>, <paramref name="y"/>) screen pixels, raises <see
    /// cref="PanChanged"/> and refreshes.
    /// </summary>
    public void SetPan(double x, double y)
    {
        Pan = new Point(x, y);
        PanChanged?.Invoke();
        Refresh();
    }

    /// <summary>
    /// Moves <see cref="Pan"/> by the given screen pixels, raises <see cref="PanChanged"/> and refreshes.
    /// </summary>
    public void UpdatePan(double deltaX, double deltaY)
    {
        Pan = Pan.Add(deltaX, deltaY);
        PanChanged?.Invoke();
        Refresh();
    }

    /// <summary>
    /// Sets <see cref="Zoom"/>, clamped to <see cref="DiagramZoomOptions.Minimum"/> and <see
    /// cref="DiagramZoomOptions.Maximum"/>, raises <see cref="ZoomChanged"/> and refreshes. <see cref="Pan"/> is left
    /// as it is, so the view zooms about the viewport origin. Throws <see cref="ArgumentException"/> when <paramref
    /// name="newZoom"/> is zero or negative.
    /// </summary>
    public void SetZoom(double newZoom)
    {
        if (newZoom <= 0)
            throw new ArgumentException($"{nameof(newZoom)} cannot be equal or lower than 0");

        newZoom = Math.Clamp(newZoom, Options.Zoom.Minimum, Options.Zoom.Maximum);

        Zoom = newZoom;
        ZoomChanged?.Invoke();
        Refresh();
    }

    /// <summary>
    /// Stores the canvas rectangle; does nothing if it equals the current one. Called by the renderer when the canvas
    /// is measured or resized.
    /// </summary>
    public void SetContainer(Rectangle newRect)
    {
        if (newRect.Equals(Container))
            return;

        Container = newRect;
        ContainerChanged?.Invoke();
        Refresh();
    }

    /// <summary>
    /// Converts a client (screen) position into diagram coordinates, undoing the container offset, <see cref="Pan"/>
    /// and <see cref="Zoom"/>. Throws if <see cref="Container"/> is not set yet.
    /// </summary>
    public Point GetRelativeMousePoint(double clientX, double clientY)
    {
        if (Container == null)
            throw new DiagramsException(
                "Container not available. Make sure you're not using this method before the diagram is fully loaded");

        return new Point((clientX - Container.Left - Pan.X) / Zoom, (clientY - Container.Top - Pan.Y) / Zoom);
    }

    /// <summary>
    /// Converts a client position into pixels relative to the canvas, without applying pan or zoom. Throws if <see
    /// cref="Container"/> is not set yet.
    /// </summary>
    public Point GetRelativePoint(double clientX, double clientY)
    {
        if (Container == null)
            throw new DiagramsException(
                "Container not available. Make sure you're not using this method before the diagram is fully loaded");

        return new Point(clientX - Container.Left, clientY - Container.Top);
    }

    /// <summary>
    /// Converts a position in diagram coordinates into client pixels; the inverse of <see
    /// cref="GetRelativeMousePoint"/>. Throws if <see cref="Container"/> is not set yet.
    /// </summary>
    public Point GetScreenPoint(double x, double y)
    {
        if (Container == null)
            throw new DiagramsException(
                "Container not available. Make sure you're not using this method before the diagram is fully loaded");

        return new Point(Zoom * x + Container.Left + Pan.X, Zoom * y + Container.Top + Pan.Y);
    }

    #region Ordering

    /// <summary>
    /// Draws <paramref name="model"/> behind every other model in its <see cref="SelectableModel.Layer"/> by
    /// renumbering all <see cref="SelectableModel.Order"/> values, then refreshes. Does nothing if the model is not
    /// in this diagram.
    /// </summary>
    public void SendToBack(SelectableModel model)
    {
        var minOrder = GetMinOrder();
        if (model.Order == minOrder)
            return;

        if (!_orderedSelectables.Remove(model))
            return;

        _orderedSelectables.Insert(0, model);

        // Todo: can optimize this by only updating the order of items before model
        Batch(() =>
        {
            SuspendSorting = true;
            for (var i = 0; i < _orderedSelectables.Count; i++)
            {
                _orderedSelectables[i].Order = i + 1;
            }
            _orderedSelectables.Sort(CompareDrawingOrder);
            SuspendSorting = false;
        });
    }

    /// <summary>
    /// Draws <paramref name="model"/> in front of every other model in its <see cref="SelectableModel.Layer"/> by
    /// giving it an <see cref="SelectableModel.Order"/> above the current maximum, then refreshes. Does nothing if
    /// the model is not in this diagram.
    /// </summary>
    public void SendToFront(SelectableModel model)
    {
        var maxOrder = GetMaxOrder();
        if (model.Order == maxOrder)
            return;

        if (!_orderedSelectables.Remove(model))
            return;

        _orderedSelectables.Add(model);

        SuspendSorting = true;
        model.Order = maxOrder + 1;
        _orderedSelectables.Sort(CompareDrawingOrder);
        SuspendSorting = false;
        Refresh();
    }

    // The list is sorted by layer first, so its ends hold the extreme orders only when every model is in one layer.
    /// <summary>
    /// The smallest <see cref="SelectableModel.Order"/> of any node, link or group, across all layers; 0 when the
    /// diagram is empty.
    /// </summary>
    public int GetMinOrder()
    {
        return _orderedSelectables.Count > 0 ? _orderedSelectables.Min(m => m.Order) : 0;
    }

    /// <summary>
    /// The largest <see cref="SelectableModel.Order"/> of any node, link or group, across all layers; 0 when the
    /// diagram is empty.
    /// </summary>
    public int GetMaxOrder()
    {
        return _orderedSelectables.Count > 0 ? _orderedSelectables.Max(m => m.Order) : 0;
    }

    /// <summary>
    /// Sorts the list of selectables in drawing order: by layer, then by order
    /// </summary>
    public void RefreshOrders(bool refresh = true)
    {
        _orderedSelectables.Sort(CompareDrawingOrder);
        
        if (refresh)
        {
            Refresh();
        }
    }

    private static int CompareDrawingOrder(SelectableModel a, SelectableModel b)
    {
        var byLayer = a.Layer.CompareTo(b.Layer);
        return byLayer != 0 ? byLayer : a.Order.CompareTo(b.Order);
    }

    private void OnSelectableAdded(SelectableModel model)
    {
        var maxOrder = GetMaxOrder();
        _orderedSelectables.Add(model);

        if (model.Order == 0)
        {
            model.Order = maxOrder + 1;
        }

        // Appending is right unless the model belongs below the current last one (a lower layer, or a preset order).
        if (_orderedSelectables.Count > 1 && CompareDrawingOrder(_orderedSelectables[^2], model) > 0)
        {
            _orderedSelectables.Sort(CompareDrawingOrder);
        }

        model.OrderChanged += OnModelOrderChanged;
    }

    private void OnSelectableRemoved(SelectableModel model)
    {
        model.OrderChanged -= OnModelOrderChanged;
        _orderedSelectables.Remove(model);
    }

    private void OnModelOrderChanged(Model model)
    {
        if (SuspendSorting)
            return;

        RefreshOrders();
    }

    #endregion

    #region Events

    /// <summary>
    /// Raises <see cref="PointerDown"/>. The renderer calls it; call it yourself to simulate input.
    /// </summary>
    public void TriggerPointerDown(Model? model, PointerEventArgs e)
    {
        using (Changes.Suspend())
            PointerDown?.Invoke(model, e);
    }

    /// <summary>Raises <see cref="PointerMove"/>.</summary>
    public void TriggerPointerMove(Model? model, PointerEventArgs e)
    {
        using (Changes.Suspend())
            PointerMove?.Invoke(model, e);
    }

    /// <summary>Raises <see cref="PointerUp"/>.</summary>
    public void TriggerPointerUp(Model? model, PointerEventArgs e)
    {
        using (Changes.Suspend())
            PointerUp?.Invoke(model, e);
    }

    /// <summary>Raises <see cref="PointerEnter"/>.</summary>
    public void TriggerPointerEnter(Model? model, PointerEventArgs e)
    {
        using (Changes.Suspend())
            PointerEnter?.Invoke(model, e);
    }

    /// <summary>Raises <see cref="PointerLeave"/>.</summary>
    public void TriggerPointerLeave(Model? model, PointerEventArgs e)
    {
        using (Changes.Suspend())
            PointerLeave?.Invoke(model, e);
    }

    /// <summary>Raises <see cref="KeyDown"/>.</summary>
    public void TriggerKeyDown(KeyboardEventArgs e)
    {
        using (Changes.Suspend())
            KeyDown?.Invoke(e);
    }

    /// <summary>Raises <see cref="Wheel"/>.</summary>
    public void TriggerWheel(WheelEventArgs e)
    {
        using (Changes.Suspend())
            Wheel?.Invoke(e);
    }

    /// <summary>
    /// Raises <see cref="PointerClick"/>; <see cref="Behaviors.EventsBehavior"/> calls it when a press and release
    /// land on the same model without movement.
    /// </summary>
    public void TriggerPointerClick(Model? model, PointerEventArgs e)
    {
        using (Changes.Suspend())
            PointerClick?.Invoke(model, e);
    }

    /// <summary>
    /// Raises <see cref="PointerDoubleClick"/>; <see cref="Behaviors.EventsBehavior"/> calls it for two clicks within
    /// 500 ms.
    /// </summary>
    public void TriggerPointerDoubleClick(Model? model, PointerEventArgs e)
    {
        using (Changes.Suspend())
            PointerDoubleClick?.Invoke(model, e);
    }

    #endregion
}