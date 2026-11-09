using Flow.Diagrams.Components.Controls;
using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Controls.Default;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Options;

namespace Flow.Diagrams;

/// <summary>
/// The <see cref="Diagram"/> that <see cref="Components.DiagramCanvas"/> renders. Adds the mapping from model types to the
/// Blazor components that draw them, and the Blazor-specific <see cref="Options"/>.
/// </summary>
public class BlazorDiagram : Diagram
{
    private readonly Dictionary<Type, Type> _componentsMapping;

    /// <summary>Creates a diagram with the default component mapping for the built-in controls.</summary>
    /// <param name="options">Options for the diagram; <c>null</c> creates a default <see cref="BlazorDiagramOptions"/>.</param>
    /// <param name="registerDefaultBehaviors">Whether to register the built-in behaviors (selection, dragging, link creation, panning, zooming, keyboard shortcuts, controls and virtualization).</param>
    public BlazorDiagram(BlazorDiagramOptions? options = null, bool registerDefaultBehaviors = true) : base(registerDefaultBehaviors)
    {
        _componentsMapping = new Dictionary<Type, Type>
        {
            [typeof(RemoveControl)] = typeof(RemoveControlWidget),
            [typeof(BoundaryControl)] = typeof(BoundaryControlWidget),
            [typeof(DragNewLinkControl)] = typeof(DragNewLinkControlWidget),
            [typeof(ArrowHeadControl)] = typeof(ArrowHeadControlWidget)
        };

        Options = options ?? new BlazorDiagramOptions();
    }

    /// <summary>The options this diagram was created with; the instance is fixed for the diagram's lifetime.</summary>
    public override BlazorDiagramOptions Options { get; }

    /// <summary>Draws every model of type <typeparamref name="TModel"/> (and, by default, its subclasses) with <typeparamref name="TComponent"/>; see <see cref="RegisterComponent(Type, Type, bool)"/>.</summary>
    public void RegisterComponent<TModel, TComponent>(bool replace = false)
    {
        RegisterComponent(typeof(TModel), typeof(TComponent), replace);
    }

    /// <summary>
    /// Draws every model of type <paramref name="modelType"/> (and, through <see cref="GetComponent(Type, bool)"/>, its subclasses)
    /// with the component <paramref name="componentType"/>. The component receives the model in a parameter named after its role:
    /// <c>Node</c>, <c>Group</c>, <c>Link</c>, <c>Label</c>, <c>Vertex</c>, or <c>Control</c> and <c>Model</c> for controls.
    /// </summary>
    /// <param name="modelType">The model type to map.</param>
    /// <param name="componentType">The component that draws it.</param>
    /// <param name="replace">Whether to overwrite an existing registration for <paramref name="modelType"/>; when <c>false</c> an existing one throws.</param>
    /// <exception cref="Exception">A component is already registered for <paramref name="modelType"/> and <paramref name="replace"/> is <c>false</c>.</exception>
    public void RegisterComponent(Type modelType, Type componentType, bool replace = false)
    {
        if (!replace && _componentsMapping.ContainsKey(modelType))
            throw new Exception($"Component already registered for model '{modelType.Name}'.");

        _componentsMapping[modelType] = componentType;
    }

    /// <summary>
    /// The component registered for <paramref name="modelType"/>, or <c>null</c> when none is, in which case the renderer falls
    /// back to its default widget.
    /// </summary>
    /// <param name="modelType">The model type to look up.</param>
    /// <param name="checkSubclasses">Whether to accept a registration for a base class of <paramref name="modelType"/> when there is no exact one; the nearest registered base class wins.</param>
    public Type? GetComponent(Type modelType, bool checkSubclasses = true)
    {
        if (_componentsMapping.ContainsKey(modelType))
            return _componentsMapping[modelType];

        if (!checkSubclasses)
            return null;

        for (var baseType = modelType.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            if (_componentsMapping.TryGetValue(baseType, out var component))
                return component;
        }

        return null;
    }

    /// <summary>The component registered for <typeparamref name="TModel"/>; see <see cref="GetComponent(Type, bool)"/>.</summary>
    public Type? GetComponent<TModel>(bool checkSubclasses = true)
    {
        return GetComponent(typeof(TModel), checkSubclasses);
    }

    /// <summary>The component registered for the runtime type of <paramref name="model"/>; see <see cref="GetComponent(Type, bool)"/>.</summary>
    public Type? GetComponent(Model model, bool checkSubclasses = true)
    {
        return GetComponent(model.GetType(), checkSubclasses);
    }
}