using System.ComponentModel;

namespace Catalog.Infrastructure;

public enum ExampleCategory
{
    [Description("Basics")] Basics,
    [Description("Nodes")] Nodes,
    [Description("Links")] Links,
    [Description("Routing")] Routing,
    [Description("Layers and labels")] Layers,
    [Description("Groups")] Groups,
    [Description("Export")] Export,
    [Description("Showcases")] Showcases,
    [Description("Performance")] Performance,
}

public static class ExampleCategoryExtensions
{
    public static string Title(this ExampleCategory category) =>
        typeof(ExampleCategory).GetField(category.ToString())!
            .GetCustomAttributes(typeof(DescriptionAttribute), false).OfType<DescriptionAttribute>().Single().Description;
}
