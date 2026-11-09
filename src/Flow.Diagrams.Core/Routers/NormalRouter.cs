using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using System.Linq;

namespace Flow.Diagrams.Core.Routers;

/// <summary>Passes through the link's own vertices as they are, in order; the default when nothing needs computing.</summary>
public class NormalRouter : Router
{
    /// <inheritdoc />
    public override Point[] GetRoute(Diagram diagram, BaseLinkModel link)
    {
        return link.Vertices.Select(v => v.Position).ToArray();
    }
}
