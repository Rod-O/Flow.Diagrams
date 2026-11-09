using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Models;

namespace Flow.Diagrams.Extensions;

/// <summary>Helpers on <see cref="Model"/> for deciding which layer draws it.</summary>
public static class ModelExtensions
{
    /// <summary>
    /// Whether the model is drawn in the SVG layer: <see cref="SvgNodeModel"/>, <see cref="SvgGroupModel"/> and every link;
    /// everything else is drawn in the HTML layer. Controls use it to pick the layer of the model they belong to.
    /// </summary>
    public static bool IsSvg(this Model model)
    {
        return model is SvgNodeModel or SvgGroupModel or BaseLinkModel;
    }
}