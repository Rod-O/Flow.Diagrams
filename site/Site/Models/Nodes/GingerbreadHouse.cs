using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Site.Models.Nodes;

public class GingerbreadHouse : SvgGroupModel
{
    public GingerbreadHouse(IEnumerable<NodeModel> children, byte padding = 30, bool autoSize = true) : base(children, padding, autoSize)
    {
    }
}
