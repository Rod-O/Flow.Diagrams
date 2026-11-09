using System;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>
/// Base of everything the diagram holds (nodes, ports, links, labels, vertices, groups): an identity, visibility and
/// lock flags, and a <see cref="Changed"/> event the renderer listens to.
/// </summary>
public abstract class Model
{
    private bool _visible = true;
    
    /// <summary>Creates a model with a new GUID as its <see cref="Id"/>.</summary>
    protected Model() : this(Guid.NewGuid().ToString()) { }

    /// <summary>
    /// Creates a model with the given <paramref name="id"/>; the library does not check that ids are unique.
    /// </summary>
    protected Model(string id)
    {
        Id = id;
    }

    /// <summary>Raised by <see cref="Refresh"/>; the renderer redraws the model in response.</summary>
    public event Action<Model>? Changed;
    /// <summary>Raised when <see cref="Visible"/> actually changes value.</summary>
    public event Action<Model>? VisibilityChanged;

    /// <summary>Identifier fixed at construction; a GUID string unless one was passed to the constructor.</summary>
    public string Id { get; }
    /// <summary>
    /// Whether the model is protected from interaction: a locked movable model is not moved by dragging, and a locked
    /// port cannot start a link or receive one. Nodes and groups also get a <c>locked</c> CSS class. Setting it does
    /// not refresh the model.
    /// </summary>
    public bool Locked { get; set; }
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
    /// Raises <see cref="Changed"/> so the model is redrawn. Overrides may recompute derived state first.
    /// </summary>
    public virtual void Refresh() => Changed?.Invoke(this);

    // Redraw without the derived-state work an override of Refresh may do, such as routing a link again.
    private protected void RaiseChanged() => Changed?.Invoke(this);
}
