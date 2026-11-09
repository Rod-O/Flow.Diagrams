using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using System.Linq;

namespace Flow.Diagrams.Core.Routers;

public class NormalRouter : Router
{
    public override Point[] GetRoute(Diagram diagram, BaseLinkModel link)
    {
        return link.Vertices.Select(v => v.Position).ToArray();
    }
}
