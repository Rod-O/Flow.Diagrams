using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Geometry;

/// <summary>A link's drawn route as a polyline, and how two links relate: same net, crossing.</summary>
internal static class LinkGeometry
{
    private const double Eps = 1e-6;

    /// <summary>The end points from the anchors with the route between them; null while the link has no route.</summary>
    public static List<Point>? Polyline(BaseLinkModel link)
    {
        if (link.Route is not { } route)
            return null;
        var a = link.Source.GetPosition(link, route);
        var b = link.Target.GetPosition(link, route);
        return a == null || b == null ? null : new[] { a }.Concat(route).Append(b).ToList();
    }

    /// <summary>Links that share a port are one net: the branches of a tee.</summary>
    public static bool SharePort(BaseLinkModel a, BaseLinkModel b) => Ports(a).Any(p => Ports(b).Contains(p));

    public static IEnumerable<(Point A, Point B)> Segments(IReadOnlyList<Point> polyline)
    {
        for (var i = 0; i + 1 < polyline.Count; i++)
            yield return (polyline[i], polyline[i + 1]);
    }

    public static bool Horizontal(Point a, Point b) => Math.Abs(a.Y - b.Y) < Eps;

    /// <summary>The interior crossing point of two perpendicular axis-parallel segments; null when they only touch or miss.</summary>
    public static Point? Crossing(Point a, Point b, Point c, Point d, double tolerance = 0.05)
    {
        var abHorizontal = Horizontal(a, b);
        var abVertical = Math.Abs(a.X - b.X) < Eps;
        var cdHorizontal = Horizontal(c, d);
        var cdVertical = Math.Abs(c.X - d.X) < Eps;
        if (!(abHorizontal && cdVertical) && !(abVertical && cdHorizontal))
            return null;

        var (h0, h1, v0, v1) = abHorizontal ? (a, b, c, d) : (c, d, a, b);
        var x = v0.X;
        var y = h0.Y;
        var inHorizontal = x > Math.Min(h0.X, h1.X) + tolerance && x < Math.Max(h0.X, h1.X) - tolerance;
        var inVertical = y > Math.Min(v0.Y, v1.Y) + tolerance && y < Math.Max(v0.Y, v1.Y) - tolerance;
        return inHorizontal && inVertical ? new Point(x, y) : null;
    }

    private static IEnumerable<PortModel> Ports(BaseLinkModel link)
    {
        if (link.Source is SinglePortAnchor s) yield return s.Port;
        if (link.Target is SinglePortAnchor t) yield return t.Port;
    }
}
