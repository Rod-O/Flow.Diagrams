using System;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>A model the user can select and that has a place in the drawing order.</summary>
public abstract class SelectableModel : Model
{
    private int _order;
    private int _layer;
    private string? _cssClass;

    /// <summary>
    /// Raised when <see cref="Order"/> or <see cref="Layer"/> changes; the diagram re-sorts its drawing order in
    /// response.
    /// </summary>
    public event Action<SelectableModel>? OrderChanged;

    /// <summary>Creates a model with a generated <see cref="Model.Id"/>.</summary>
    protected SelectableModel() { }

    /// <summary>Creates a model with the given <paramref name="id"/>.</summary>
    protected SelectableModel(string id) : base(id) { }

    /// <summary>
    /// Whether the model is selected. Read-only from outside the library; select through <see
    /// cref="Diagram.SelectModel"/> and <see cref="Diagram.UnselectModel"/>.
    /// </summary>
    public bool Selected { get; internal set; }
    /// <summary>
    /// Extra CSS classes, space-separated, that the renderer adds to the model's wrapper element next to the classes
    /// the library sets (<c>selected</c>, <c>locked</c>, ...), so an app can style and animate a model with plain CSS;
    /// <c>diagram-pulse</c> is a built-in highlight. Rendered for nodes, groups and links, and kept in the SVG export.
    /// Changing it redraws the model; <c>null</c> adds nothing.
    /// </summary>
    public string? CssClass
    {
        get => _cssClass;
        set
        {
            if (value == _cssClass)
                return;

            _cssClass = value;
            RaiseChanged();
        }
    }
    /// <summary>
    /// Position in the drawing order within the <see cref="Layer"/>; higher is drawn in front. A model added with 0
    /// is given the highest order plus one. Changing it raises <see cref="OrderChanged"/>; <see
    /// cref="Diagram.SendToBack"/> and <see cref="Diagram.SendToFront"/> are the usual way to change it.
    /// </summary>
    public int Order
    {
        get => _order;
        set
        {
            if (value == Order)
                return;

            _order = value;
            OrderChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// The drawing band: a model in a higher layer is drawn above every model of a lower one, and <see cref="Order"/>
    /// decides within a layer. Changing it raises <see cref="OrderChanged"/>, since the drawing order changes.
    /// </summary>
    public int Layer
    {
        get => _layer;
        set
        {
            if (value == _layer)
                return;

            _layer = value;
            OrderChanged?.Invoke(this);
        }
    }
}
