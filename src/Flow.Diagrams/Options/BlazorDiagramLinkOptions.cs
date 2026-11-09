using Flow.Diagrams.Core.Options;

namespace Flow.Diagrams.Options;

/// <summary>Link options of a <see cref="BlazorDiagram"/>: the core options plus the colors the default link widgets use.</summary>
public class BlazorDiagramLinkOptions : DiagramLinkOptions
{
    /// <summary>CSS color of links, of their markers and of the default link label text, for links whose <c>Color</c> is <c>null</c>.</summary>
    public string DefaultColor { get; set; } = "black";
    /// <summary>CSS color of selected links and their markers, for links whose <c>SelectedColor</c> is <c>null</c>.</summary>
    public string DefaultSelectedColor { get; set; } = "rgb(110, 159, 212)";
}