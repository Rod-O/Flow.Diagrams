using System.Collections.Generic;
using Flow.Diagrams.Core.Models;

namespace Flow.Diagrams.Models;

/// <summary>A group drawn in the diagram's SVG layer; use it for groups of <see cref="SvgNodeModel"/>. See <see cref="GroupModel"/> for sizing and padding.</summary>
public class SvgGroupModel : GroupModel
{
    /// <summary>Creates a group around <paramref name="children"/> with the same padding, in diagram units, on every side.</summary>
    public SvgGroupModel(IEnumerable<NodeModel> children, byte padding = 30, bool autoSize = true) : base(children, padding, autoSize)
    {
    }

    /// <summary>Creates a group around <paramref name="children"/> with a padding per side.</summary>
    public SvgGroupModel(IEnumerable<NodeModel> children, GroupPadding padding, bool autoSize = true) : base(children, padding, autoSize)
    {
    }
}