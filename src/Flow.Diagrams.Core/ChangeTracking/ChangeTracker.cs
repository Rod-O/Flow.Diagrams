using Flow.Diagrams.Core.Anchors;
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
    private Dictionary<Model, ModelChanges> _pending = new();
    private List<Model> _passOrder = new();
    private Dictionary<Model, ModelChanges> _passChanges = new();
    private readonly HashSet<BaseLinkModel> _routed = new();
    private Exception? _firstFailure;
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

    /// <summary>
    /// Defers flushing until the returned handle is disposed; suspensions nest, and the outermost flushes. The handle
    /// is a struct: dispose it exactly once.
    /// </summary>
    public Suspension Suspend()
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
        _firstFailure = null;
        try
        {
            for (var pass = 1; pass <= MaxPasses && _order.Count > 0; pass++)
            {
                // The previous pass left its buffers empty; they collect the marks of this one.
                (_passOrder, _order) = (_order, _passOrder);
                (_passChanges, _pending) = (_pending, _passChanges);

                // Links route against the geometry of the nodes and groups they attach to, so those go first.
                UpdateNodeGeometry();
                UpdateLinkGeometry();

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
                        _firstFailure ??= e;
                    }
                }

                _passOrder.Clear();
                _passChanges.Clear();
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
            _passOrder.Clear();
            _passChanges.Clear();
            _routed.Clear();
            IsFlushing = false;
        }

        var failure = _firstFailure;
        _firstFailure = null;
        if (failure != null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void UpdateNodeGeometry()
    {
        _phase = Phase.NodeGeometry;
        // Indexed, because links marked while the nodes update are appended to this pass.
        for (var i = 0; i < _passOrder.Count; i++)
        {
            var model = _passOrder[i];
            if (model is not BaseLinkModel && NeedsGeometry(model))
                UpdateGeometry(model);
        }
    }

    private void UpdateLinkGeometry()
    {
        _phase = Phase.LinkGeometry;
        for (var i = 0; i < _passOrder.Count; i++)
        {
            if (_passOrder[i] is BaseLinkModel link && NeedsGeometry(link))
                Route(link);
        }

        _routed.Clear();
    }

    /// <summary>Routes <paramref name="link"/> once per pass, after the pending links its anchors sit on.</summary>
    private void Route(BaseLinkModel link)
    {
        // Added before the hosts are routed, so links anchored on each other in a cycle stop here.
        if (!_routed.Add(link))
            return;

        RouteHost(link.Source);
        RouteHost(link.Target);
        UpdateGeometry(link);
    }

    private void RouteHost(Anchor anchor)
    {
        if (anchor is LinkAnchor { Link: var host } && _passChanges.ContainsKey(host) && NeedsGeometry(host))
            Route(host);
    }

    private bool NeedsGeometry(Model model) =>
        model.Tracker == this && _passChanges[model].HasFlag(ModelChanges.Geometry);

    private void UpdateGeometry(Model model)
    {
        _updating = model;
        try
        {
            model.UpdateGeometry();
        }
        catch (Exception e)
        {
            _firstFailure ??= e;
        }
        finally
        {
            _updating = null;
        }
    }

    /// <summary>A suspension of a <see cref="ChangeTracker"/>; disposing it resumes the tracker, which flushes when it is the outermost.</summary>
    internal readonly struct Suspension : IDisposable
    {
        private readonly ChangeTracker? _tracker;

        public Suspension(ChangeTracker tracker) => _tracker = tracker;

        public void Dispose() => _tracker?.Resume();

        /// <summary>
        /// Resumes like <see cref="Dispose"/>, discarding an exception from the flush, so the exception already in
        /// flight is the one the caller sees.
        /// </summary>
        public void DisposeAfterFailure()
        {
            try
            {
                Dispose();
            }
            catch (Exception)
            {
                // The caller rethrows its own exception.
            }
        }
    }
}
