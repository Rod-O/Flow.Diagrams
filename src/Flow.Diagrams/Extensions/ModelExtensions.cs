using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Models;

namespace Flow.Diagrams.Extensions;

public static class ModelExtensions
{
    public static bool IsSvg(this Model model)
    {
        return model is SvgNodeModel or SvgGroupModel or BaseLinkModel;
    }
}