using System.Collections.Generic;
using Flow.Diagrams.Core.Models;

namespace Flow.Diagrams.Models;

public class SvgGroupModel : GroupModel
{
    public SvgGroupModel(IEnumerable<NodeModel> children, byte padding = 30, bool autoSize = true) : base(children, padding, autoSize)
    {
    }

    public SvgGroupModel(IEnumerable<NodeModel> children, GroupPadding padding, bool autoSize = true) : base(children, padding, autoSize)
    {
    }
}