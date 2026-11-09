namespace Catalog.Infrastructure;

/// <summary>Marks a component as a catalog example; the gallery finds every component that carries it.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ExampleAttribute(string id, string title, ExampleCategory category, string description) : Attribute
{
    /// <summary>The example's URL segment and preview file name: lowercase words joined by dashes.</summary>
    public string Id { get; } = id;
    public string Title { get; } = title;
    public ExampleCategory Category { get; } = category;
    public string Description { get; } = description;

    /// <summary>Order within the category; lower first.</summary>
    public int Order { get; init; }

    /// <summary>Marks examples of what Flow.Diagrams added since the fork.</summary>
    public bool New { get; init; }
}
