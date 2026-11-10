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
    private int _depth;

    /// <summary>Whether a flush is running; marks made meanwhile are raised in a further pass of the same flush.</summary>
    public bool IsFlushing { get; private set; }

    /// <summary>
    /// Adds <paramref name="changes"/> to what is pending for <paramref name="model"/>; the first geometry mark of a
    /// model in a pass also marks its dependents. Flushes at once when not suspended and not already flushing.
    /// </summary>
    public void Mark(Model model, ModelChanges changes)
    {
        if (_pending.TryGetValue(model, out var pending))
        {
            _pending[model] = pending | changes;
        }
        else
        {
            pending = ModelChanges.None;
            _pending[model] = changes;
            _order.Add(model);
        }

        if (changes.HasFlag(ModelChanges.Geometry) && !pending.HasFlag(ModelChanges.Geometry))
            model.PropagateGeometry(this);

        if (_depth == 0 && !IsFlushing)
            Flush();
    }

    /// <summary>Defers flushing until the returned handle is disposed; suspensions nest, and the outermost flushes.</summary>
    public IDisposable Suspend()
    {
        _depth++;
        return new Suspension(this);
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
                var order = _order;
                var changes = _pending;
                _order = new List<Model>();
                _pending = new Dictionary<Model, ModelChanges>(ReferenceEqualityComparer.Instance);

                // Links route against the geometry of the nodes and groups they attach to, so those go first.
                UpdateGeometry(order, changes, links: false);
                UpdateGeometry(order, changes, links: true);

                foreach (var model in order)
                {
                    if (model.Tracker != this)
                        continue;

                    try
                    {
                        model.RaiseChanged(changes[model] | ModelChanges.Appearance);
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
            IsFlushing = false;
        }

        if (firstFailure != null)
            ExceptionDispatchInfo.Capture(firstFailure).Throw();
    }

    private void UpdateGeometry(List<Model> order, Dictionary<Model, ModelChanges> changes, bool links)
    {
        foreach (var model in order)
        {
            if (model is BaseLinkModel != links || model.Tracker != this)
                continue;

            if (changes[model].HasFlag(ModelChanges.Geometry))
                model.UpdateGeometry();
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
