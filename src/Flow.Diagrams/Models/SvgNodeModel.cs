using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;

namespace Flow.Diagrams.Models;

public class SvgNodeModel : NodeModel
{
    public SvgNodeModel(Point? position = null) : base(position)
    {
    }

    public SvgNodeModel(string id, Point? position = null) : base(id, position)
    {
    }
}