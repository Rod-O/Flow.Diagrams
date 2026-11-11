using System;
using System.Collections.Generic;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Controls;

/// <summary>The controls of every model in a diagram, reachable as <see cref="Diagram.Controls"/>. The renderer redraws a model's controls when <see cref="ChangeCaused"/> fires for it.</summary>
public class ControlsLayer
{
    private readonly Dictionary<Model, ControlsContainer> _containers;

    /// <summary>Raised with the affected model when its controls were shown or hidden, changed or moved while visible, or removed.</summary>
    public event Action<Model>? ChangeCaused;

    /// <summary>Creates an empty layer.</summary>
    public ControlsLayer()
    {
        _containers = new Dictionary<Model, ControlsContainer>();
    }

    /// <summary>The models that have a <see cref="ControlsContainer"/>.</summary>
    public IReadOnlyCollection<Model> Models => _containers.Keys;

    /// <summary>Gives <paramref name="model"/> a container, or returns the one it already has unchanged (<paramref name="type"/> is then ignored).</summary>
    public ControlsContainer AddFor(Model model, ControlsType type = ControlsType.OnSelection)
    {
        if (_containers.ContainsKey(model))
            return _containers[model];
        
        var container = new ControlsContainer(model, type);
        container.VisibilityChanged += OnVisibilityChanged;
        container.ControlsChanged += RefreshIfVisible;
        model.Changed += RefreshIfVisible;
        _containers.Add(model, container);
        return container;
    }

    /// <summary>The container of <paramref name="model"/>; null when it has none.</summary>
    public ControlsContainer? GetFor(Model model)
    {
        return _containers.TryGetValue(model, out var container) ? container : null;
    }

    /// <summary>Removes the container of <paramref name="model"/>, raising <see cref="ChangeCaused"/>; false when it had none.</summary>
    public bool RemoveFor(Model model)
    {
        if (!_containers.TryGetValue(model, out var container))
            return false;
        
        container.VisibilityChanged -= OnVisibilityChanged;
        container.ControlsChanged -= RefreshIfVisible;
        model.Changed -= RefreshIfVisible;
        _containers.Remove(model);
        ChangeCaused?.Invoke(model);
        return true;
    }

    /// <summary>Whether <paramref name="model"/> has a container whose controls are shown.</summary>
    public bool AreVisibleFor(Model model) => GetFor(model)?.Visible ?? false;

    private void RefreshIfVisible(Model cause, ModelChanges changes) => RefreshIfVisible(cause);

    private void RefreshIfVisible(Model cause)
    {
        if (!AreVisibleFor(cause))
            return;
        
        ChangeCaused?.Invoke(cause);
    }

    private void OnVisibilityChanged(Model cause) => ChangeCaused?.Invoke(cause);
}