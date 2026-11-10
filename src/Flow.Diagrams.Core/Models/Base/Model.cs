using Flow.Diagrams.Core.ChangeTracking;
using System;
using System.Collections.Generic;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>
/// Base of everything the diagram holds (nodes, ports, links, labels, vertices, groups): an identity, visibility and
/// lock flags, and a <see cref="Changed"/> event the renderer listens to.
/// </summary>
public abstract class Model
{
    private bool _visible = true;
    private bool _locked;
    private int _suspended;
    private ModelChanges _held;
    private ChangeTracker? _tracker;
    
    /// <summary>Creates a model with a new GUID as its <see cref="Id"/>.</summary>
    protected Model() : this(Guid.NewGuid().ToString()) { }

    /// <summary>
    /// Creates a model with the given <paramref name="id"/>; the library does not check that ids are unique.
    /// </summary>
    protected Model(string id)
    {
        Id = id;
    }

    /// <summary>Raised by <see cref="MarkChanged"/> with what changed; the renderer redraws the model in response.</summary>
    public event Action<Model, ModelChanges>? Changed;
    /// <summary>Raised when <see cref="Visible"/> actually changes value.</summary>
    public event Action<Model>? VisibilityChanged;

    /// <summary>Identifier fixed at construction; a GUID string unless one was passed to the constructor.</summary>
    public string Id { get; }
    /// <summary>
    /// Whether the model is protected from interaction: a locked movable model is not moved by dragging, and a locked
    /// port cannot start a link or receive one. Nodes and groups also get a <c>locked</c> CSS class. A change raises
    /// <see cref="Changed"/> with <see cref="ModelChanges.Appearance"/>.
    /// </summary>
    public bool Locked
    {
        get => _locked;
        set => SetField(ref _locked, value, ModelChanges.Appearance);
    }
    /// <summary>
    /// Whether the model is rendered; raises <see cref="VisibilityChanged"/> when it changes. With <see
    /// cref="Options.DiagramVirtualizationOptions"/> enabled, virtualization overwrites it for the models it covers.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
                return;

            _visible = value;
            VisibilityChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Recomputes derived geometry, such as a link's route, and raises <see cref="Changed"/> so the model is redrawn.
    /// </summary>
    public virtual void Refresh() => MarkChanged(ModelChanges.Appearance | ModelChanges.Geometry);

    /// <summary>
    /// Raises <see cref="Changed"/> with <paramref name="changes"/> plus <see cref="ModelChanges.Appearance"/>; when
    /// <paramref name="changes"/> has <see cref="ModelChanges.Geometry"/>, derived geometry is recomputed first and
    /// the models that depend on this one are marked too. In a diagram the raise is deferred to the end of the
    /// current <see cref="Diagram.Batch"/>, once per model with the union of its changes; outside a batch, or outside
    /// a diagram, it happens before this method returns.
    /// </summary>
    public void MarkChanged(ModelChanges changes)
    {
        if (Tracker is { } tracker)
        {
            tracker.Mark(this, changes);
            return;
        }

        if (_suspended > 0)
        {
            _held |= changes;
            return;
        }

        if (changes.HasFlag(ModelChanges.Geometry))
            UpdateGeometry();

        RaiseChanged(changes | ModelChanges.Appearance);
    }

    /// <summary>Recomputes the state derived from other models, such as a link's route; does nothing by default.</summary>
    protected internal virtual void UpdateGeometry() { }

    /// <summary>
    /// The tracker of the diagram whose layer holds this model, or <c>null</c> outside a diagram. Ports, labels and
    /// vertices use their parent's.
    /// </summary>
    internal virtual ChangeTracker? Tracker => _tracker;

    /// <summary>
    /// Defers what this model marks until the returned handle is disposed, so a method that changes several
    /// properties raises and recomputes once. In a diagram this is the tracker's suspension; elsewhere it holds back
    /// this model's own marks.
    /// </summary>
    internal IDisposable Suspend() => Tracker?.Suspend() ?? new LocalSuspension(this);

    /// <summary>Set by a layer when it adds this model, and cleared when it removes it.</summary>
    internal void AttachTracker(ChangeTracker? tracker) => _tracker = tracker;

    /// <summary>Marks the models whose geometry derives from this one's; none by default.</summary>
    internal virtual void PropagateGeometry(ChangeTracker tracker) { }

    /// <summary>Raises <see cref="Changed"/> with exactly <paramref name="changes"/>.</summary>
    internal void RaiseChanged(ModelChanges changes) => Changed?.Invoke(this, changes);

    /// <summary>
    /// Assigns <paramref name="value"/> to <paramref name="field"/> and calls <see cref="MarkChanged"/> with
    /// <paramref name="changes"/>; returns <c>false</c> and does nothing when the value is already equal.
    /// </summary>
    protected bool SetField<T>(ref T field, T value, ModelChanges changes)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        MarkChanged(changes);
        return true;
    }

    private sealed class LocalSuspension : IDisposable
    {
        private Model? _model;

        public LocalSuspension(Model model)
        {
            _model = model;
            model._suspended++;
        }

        public void Dispose()
        {
            var model = _model;
            _model = null;
            if (model == null || --model._suspended > 0 || model._held == ModelChanges.None)
                return;

            var held = model._held;
            model._held = ModelChanges.None;
            model.MarkChanged(held);
        }
    }
}
