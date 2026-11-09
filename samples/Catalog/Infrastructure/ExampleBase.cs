using Flow.Diagrams;
using Microsoft.AspNetCore.Components;

namespace Catalog.Infrastructure;

/// <summary>An example owns one diagram; the example page exports it.</summary>
public abstract class ExampleBase : ComponentBase
{
    public BlazorDiagram Diagram { get; protected set; } = new();
}
