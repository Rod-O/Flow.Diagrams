using System;
using System.Collections;
using System.Collections.Generic;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Controls;

/// <summary>The controls of one model, plus whether they are currently shown. Created with <see cref="ControlsLayer.AddFor"/>.</summary>
public class ControlsContainer : IReadOnlyList<Control>
{
    private readonly List<Control> _controls = new(4);

    /// <summary>Raised with <see cref="Model"/> when <see cref="Show"/> or <see cref="Hide"/> changes <see cref="Visible"/>.</summary>
    public event Action<Model>? VisibilityChanged;
    /// <summary>Raised with <see cref="Model"/> after a control is added or removed.</summary>
    public event Action<Model>? ControlsChanged;

    /// <summary>Creates an empty, hidden container for <paramref name="model"/>; <paramref name="type"/> says when <see cref="ControlsBehavior"/> shows it.</summary>
    public ControlsContainer(Model model, ControlsType type = ControlsType.OnSelection)
    {
        Model = model;
        Type = type;
    }

    /// <summary>The model the controls belong to.</summary>
    public Model Model { get; }
    /// <summary>When <see cref="ControlsBehavior"/> shows the controls. Changing it does not show or hide them right away.</summary>
    public ControlsType Type { get; set; }
    /// <summary>Whether the controls are currently shown; changed by <see cref="Show"/> and <see cref="Hide"/>.</summary>
    public bool Visible { get; private set; }

    /// <summary>Shows the controls; raises <see cref="VisibilityChanged"/> only if they were hidden.</summary>
    public void Show()
    {
        if (Visible)
            return;
        
        Visible = true;
        VisibilityChanged?.Invoke(Model);
    }

    /// <summary>Hides the controls; raises <see cref="VisibilityChanged"/> only if they were shown.</summary>
    public void Hide()
    {
        if (!Visible)
            return;
        
        Visible = false;
        VisibilityChanged?.Invoke(Model);
    }

    /// <summary>Appends <paramref name="control"/> and raises <see cref="ControlsChanged"/>; returns this container for chaining.</summary>
    public ControlsContainer Add(Control control)
    {
        _controls.Add(control);
        ControlsChanged?.Invoke(Model);
        return this;
    }

    /// <summary>Removes <paramref name="control"/>, raising <see cref="ControlsChanged"/> only if it was present; returns this container for chaining.</summary>
    public ControlsContainer Remove(Control control)
    {
        if (_controls.Remove(control))
        {
            ControlsChanged?.Invoke(Model);
        }

        return this;
    }

    /// <inheritdoc />
    public int Count => _controls.Count;
    /// <inheritdoc />
    public Control this[int index] => _controls[index];
    /// <inheritdoc />
    public IEnumerator<Control> GetEnumerator() => _controls.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _controls.GetEnumerator();
}