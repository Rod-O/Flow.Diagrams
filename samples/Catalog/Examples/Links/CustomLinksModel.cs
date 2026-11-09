using Flow.Diagrams.Core.Models;

namespace Catalog.Examples.Links;

public enum CustomLinksMedium { Copper, Fiber, Wireless }

#region model
/// <summary>A network connection; the widget draws it according to its medium.</summary>
public class CustomLinksModel(PortModel source, PortModel target, CustomLinksMedium medium) : LinkModel(source, target)
{
    public CustomLinksMedium Medium { get; } = medium;
}
#endregion
