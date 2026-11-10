using Flow.Diagrams.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Flow.Diagrams.Core.ChangeTracking;

/// <summary>
/// Collects the changes marked on a diagram's models and raises each model's <see cref="Model.Changed"/> once per
/// flush, with the union of its changes, after recomputing its geometry once.
/// </summary>
internal sealed class ChangeTracker
{
    /// <summary>How many passes a flush runs before it gives up on handlers that keep marking models.</summary>
    public const int MaxPasses = 64;

    private List<Model> _order = new();
    private Dictionary<Model, ModelChanges> _pending = new(ReferenceEqualityComparer.Instance);
    private List<Model> _passOrder = new();
    private Dictionary<Model, ModelChanges> _passChanges = new(ReferenceEqualityComparer.Instance);
    private Phase _phase;
    private Model? _updating;
    private int _depth;

    private enum Phase { Idle, NodeGeometry, LinkGeometry, Raise }

    /// <summary>Whether a flush is running; marks made meanwhile are raised in a further pass of the same flush.</summary>
    public bool IsFlushing { get; private set; }

    /// <summary>
    /// Adds <paramref name="changes"/> to what is pending for <paramref name="model"/>; the first geometry mark of a
    /// model in a pass also marks its dependents. Flushes at once when not suspended and not already flushing. While
    /// geometry is being recomputed, a model marking itself and a link marked before the links are routed join the
    /// current pass, so neither is recomputed or raised twice.
    /// </summary>
    public void Mark(Model model, ModelChanges changes)
    {
        if (_phase is Phase.NodeGeometry or Phase.LinkGeometry && ReferenceEquals(model, _updating))
        {
            _passChanges[model] |= changes;
            if (changes.HasFlag(ModelChanges.Geometry))
                model.PropagateGeometry(this);
            return;
        }

        if (_phase == Phase.NodeGeometry && model is BaseLinkModel)
        {
            Add(_passOrder, _passChanges, model, changes);
            return;
        }

        Add(_order, _pending, model, changes);

        if (_depth == 0 && !IsFlushing)
            Flush();
    }

    /// <summary>Defers flushing until the returned handle is disposed; suspensions nest, and the outermost flushes.</summary>
    public IDisposable Suspend()
    {
        _depth++;
        return new Suspension(this);
    }

    private void Add(List<Model> order, Dictionary<Model, ModelChanges> pending, Model model, ModelChanges changes)
    {
        if (pending.TryGetValue(model, out var previous))
        {
            pending[model] = previous | changes;
        }
        else
        {
            previous = ModelChanges.None;
            pending[model] = changes;
            order.Add(model);
        }

        if (changes.HasFlag(ModelChanges.Geometry) && !previous.HasFlag(ModelChanges.Geometry))
            model.PropagateGeometry(this);
    }

    private void Resume()
    {
        _depth--;
        if (_depth == 0 && !IsFlushing)
            Flush();
    }

    private void Flush()
    {
        IsFlushing = true;
        Exception? firstFailure = null;
        try
        {
            for (var pass = 1; pass <= MaxPasses && _order.Count > 0; pass++)
            {
                _passOrder = _order;
                _passChanges = _pending;
                _order = new List<Model>();
                _pending = new Dictionary<Model, ModelChanges>(ReferenceEqualityComparer.Instance);

                // Links route against the geometry of the nodes and groups they attach to, so those go first.
                UpdateGeometry(Phase.NodeGeometry);
                UpdateGeometry(Phase.LinkGeometry);

                _phase = Phase.Raise;
                for (var i = 0; i < _passOrder.Count; i++)
                {
                    var model = _passOrder[i];
                    if (model.Tracker != this)
                        continue;

                    try
                    {
                        model.RaiseChanged(_passChanges[model] | ModelChanges.Appearance);
                    }
                    catch (Exception e)
                    {
                        firstFailure ??= e;
                    }
                }
            }

            if (_order.Count > 0)
            {
                _order.Clear();
                _pending.Clear();
                throw new DiagramsException(
                    $"Model changes did not settle after {MaxPasses} passes; a Changed handler keeps marking models.");
            }
        }
        finally
        {
            _phase = Phase.Idle;
            _updating = null;
            _passOrder = new List<Model>();
            _passChanges = new Dictionary<Model, ModelChanges>(ReferenceEqualityComparer.Instance);
            IsFlushing = false;
        }

        if (firstFailure != null)
            ExceptionDispatchInfo.Capture(firstFailure).Throw();
    }

    private void UpdateGeometry(Phase phase)
    {
        _phase = phase;
        var links = phase == Phase.LinkGeometry;
        // Indexed, because links marked while the nodes update are appended to this pass.
        for (var i = 0; i < _passOrder.Count; i++)
        {
            var model = _passOrder[i];
            if (model is BaseLinkModel != links || model.Tracker != this)
                continue;

            if (!_passChanges[model].HasFlag(ModelChanges.Geometry))
                continue;

            _updating = model;
            try
            {
                model.UpdateGeometry();
            }
            finally
            {
                _updating = null;
            }
        }
    }

    private sealed class Suspension : IDisposable
    {
        private ChangeTracker? _tracker;

        public Suspension(ChangeTracker tracker) => _tracker = tracker;

        public void Dispose()
        {
            var tracker = _tracker;
            _tracker = null;
            tracker?.Resume();
        }
    }
}
