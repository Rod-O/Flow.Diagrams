using System.Reflection;

namespace Catalog.Infrastructure;

public sealed record ExampleInfo(ExampleAttribute Meta, Type Component)
{
    public string Id => Meta.Id;
    public string PreviewUrl => $"previews/{Id}.png";
}

/// <summary>Every example in this assembly, found by its <see cref="ExampleAttribute"/>.</summary>
public static class ExampleRegistry
{
    public static IReadOnlyList<ExampleInfo> All { get; } = typeof(ExampleRegistry).Assembly.GetTypes()
        .Select(t => t.GetCustomAttribute<ExampleAttribute>() is { } meta ? new ExampleInfo(meta, t) : null)
        .OfType<ExampleInfo>()
        .OrderBy(e => e.Meta.Category).ThenBy(e => e.Meta.Order).ThenBy(e => e.Meta.Title)
        .ToList();

    public static ExampleInfo? Find(string id) => All.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// The example's source files: those whose name starts with the component's name, unless another example's longer
    /// name matches too ("LabelsOnTop.razor" belongs to LabelsOnTop, not to Labels).
    /// </summary>
    public static IReadOnlyList<(string Name, string Text)> Source(ExampleInfo example)
    {
        var assembly = typeof(ExampleRegistry).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("source/", StringComparison.Ordinal) && Owner(n["source/".Length..]) == example)
            .OrderBy(n => n.EndsWith(".razor") ? 0 : 1).ThenBy(n => n)
            .Select(n =>
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(n)!);
                return (n["source/".Length..], reader.ReadToEnd());
            })
            .ToList();
    }

    private static ExampleInfo? Owner(string fileName) => All
        .Where(e => fileName.StartsWith(e.Component.Name, StringComparison.Ordinal))
        .MaxBy(e => e.Component.Name.Length);
}
