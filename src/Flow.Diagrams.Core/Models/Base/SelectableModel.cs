using System;

namespace Flow.Diagrams.Core.Models.Base;

public abstract class SelectableModel : Model
{
    private int _order;
    private int _layer;

    public event Action<SelectableModel>? OrderChanged;

    protected SelectableModel() { }

    protected SelectableModel(string id) : base(id) { }

    public bool Selected { get; internal set; }
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
