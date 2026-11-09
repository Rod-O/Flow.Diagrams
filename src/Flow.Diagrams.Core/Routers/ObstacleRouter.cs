using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Routers;

/// <summary>
/// Orthogonal routing around every node on the diagram, not only the link's two ends. Each end leaves its node along
/// the side its port is aligned to; between the two stubs the route runs on a grid of lines taken from the inflated
/// node edges and the channels between them, choosing the path of least length + bends + crossings, and paying heavily
/// to run on top of a link of another net. Links that share a port are one net (the branches of a tee) and may share
/// a line.
/// </summary>
/// <remarks>
/// Each link is routed against the routes the other links have now, so the result depends on the order links are
/// routed in. <see cref="RouteAll"/> re-routes every link a few times, which undoes most corners the first pass
/// paints itself into.
/// </remarks>
public class ObstacleRouter : Router
{
    private const double Eps = 1e-6;
    private static readonly Direction[] Directions = { Direction.Left, Direction.Right, Direction.Up, Direction.Down };

    private readonly Router _fallbackRouter;

    /// <summary>Creates the router.</summary>
    /// <param name="options">The routing costs; defaults when null.</param>
    /// <param name="fallbackRouter">Used when a link has an end this router cannot route (not a port anchor on a measured node); <see cref="NormalRouter"/> when null.</param>
    public ObstacleRouter(ObstacleRouterOptions? options = null, Router? fallbackRouter = null)
    {
        Options = options ?? new ObstacleRouterOptions();
        _fallbackRouter = fallbackRouter ?? new NormalRouter();
    }

    /// <summary>The costs and clearances this router was created with.</summary>
    public ObstacleRouterOptions Options { get; }

    /// <summary>Routes every link that uses this router, then re-routes each against all the others.</summary>
    public void RouteAll(Diagram diagram, int reroutePasses = 2)
    {
        var links = diagram.Links.Where(l => ReferenceEquals(l.Router ?? diagram.Options.Links.DefaultRouter, this)).ToList();
        diagram.Batch(() =>
        {
            for (var pass = 0; pass <= reroutePasses; pass++)
                foreach (var link in links)
                    link.Refresh();
        });
    }

    /// <inheritdoc />
    /// <remarks>Returns an empty route when the two ports face each other on one line with nothing in between.</remarks>
    public override Point[] GetRoute(Diagram diagram, BaseLinkModel link)
    {
        if (ResolveEnd(link, link.Source) is not { } from || ResolveEnd(link, link.Target) is not { } to)
            return _fallbackRouter.GetRoute(diagram, link);

        var obstacles = GetObstacles(diagram, link).ToList();
        if (Facing(from, to) && !obstacles.Any(o => !o.Equals(from.Owner) && !o.Equals(to.Owner) && CutBy(o, from.At, to.At)))
            return Array.Empty<Point>();

        var s = StubEnd(from, 1);
        var t = StubEnd(to, link.TargetMarker != null ? Options.MinStub : 1);
        var blocks = obstacles
            .Select(b => b.Inflate(Options.Margin, Options.Margin))
            .Where(b => !StrictlyContains(b, s) && !StrictlyContains(b, t))
            .ToList();

        var xs = Axis(blocks.SelectMany(b => new[] { b.Left, b.Right }), s.X, t.X);
        var ys = Axis(blocks.SelectMany(b => new[] { b.Top, b.Bottom }), s.Y, t.Y);
        var placed = PlacedRoutes(diagram, link);
        var path = Search(xs, ys, blocks, s, from.Exit, t, Opposite(to.Exit), placed)
                   // No grid path: an honest dogleg, visibly wrong, rather than nothing.
                   ?? new List<Point> { s, new(t.X, s.Y), t };

        var route = Simplify(new[] { from.At }.Concat(path).Append(to.At));
        return route.Skip(1).Take(route.Count - 2).ToArray();
    }

    /// <summary>
    /// The rectangles routes keep clear of. By default every visible, measured node that is not a group: a group's
    /// frame is not an obstacle, its children are.
    /// </summary>
    protected virtual IEnumerable<Rectangle> GetObstacles(Diagram diagram, BaseLinkModel link) =>
        diagram.Nodes.Where(n => n is not GroupModel && n.Visible).Select(n => n.GetBounds()).OfType<Rectangle>();

    /// <summary>Whether two links may share a line. By default, links that share a port.</summary>
    protected virtual bool SameNet(BaseLinkModel a, BaseLinkModel b) => LinkGeometry.SharePort(a, b);

    private End? ResolveEnd(BaseLinkModel link, Anchor anchor)
    {
        if (anchor is not SinglePortAnchor { Port: var port } || port.Parent.GetBounds() is not { } owner)
            return null;

        // The anchor decides where the line really ends (a port's shape may put it off the port's box).
        if (anchor.GetPosition(link, Array.Empty<Point>()) is not { } at)
            return null;

        var exit = port.Alignment switch
        {
            PortAlignment.Top => Direction.Up,
            PortAlignment.Bottom => Direction.Down,
            PortAlignment.Left or PortAlignment.TopLeft or PortAlignment.BottomLeft => Direction.Left,
            _ => Direction.Right,
        };
        return new End(at, exit, owner);
    }

    /// <summary>Two ports on one line, each facing the other: a pipe through inline symbols, drawn straight when nothing is in between.</summary>
    private static bool Facing(End a, End b) => (a.Exit, b.Exit) switch
    {
        (Direction.Right, Direction.Left) => Math.Abs(a.At.Y - b.At.Y) < Eps && a.At.X <= b.At.X,
        (Direction.Left, Direction.Right) => Math.Abs(a.At.Y - b.At.Y) < Eps && a.At.X >= b.At.X,
        (Direction.Down, Direction.Up) => Math.Abs(a.At.X - b.At.X) < Eps && a.At.Y <= b.At.Y,
        (Direction.Up, Direction.Down) => Math.Abs(a.At.X - b.At.X) < Eps && a.At.Y >= b.At.Y,
        _ => false,
    };

    private Point StubEnd(End e, double stub) => e.Exit switch
    {
        Direction.Left => new(Math.Min(e.Owner.Left - Options.Margin, e.At.X - stub), e.At.Y),
        Direction.Right => new(Math.Max(e.Owner.Right + Options.Margin, e.At.X + stub), e.At.Y),
        Direction.Up => new(e.At.X, Math.Min(e.Owner.Top - Options.Margin, e.At.Y - stub)),
        _ => new(e.At.X, Math.Max(e.Owner.Bottom + Options.Margin, e.At.Y + stub)),
    };

    /// <summary>Grid lines: node edges, the two stubs, the middle of every channel, and a ring outside everything.</summary>
    private List<double> Axis(IEnumerable<double> edges, double a, double b)
    {
        var raw = edges.Append(a).Append(b).Distinct().OrderBy(v => v).ToList();
        var lines = new SortedSet<double>(raw);
        for (var i = 0; i + 1 < raw.Count; i++)
            lines.Add((raw[i] + raw[i + 1]) / 2);
        lines.Add(raw[0] - Options.OuterRing);
        lines.Add(raw[^1] + Options.OuterRing);
        return lines.ToList();
    }

    /// <summary>The full polylines of the other links already routed that are not of this link's net.</summary>
    private List<List<Point>> PlacedRoutes(Diagram diagram, BaseLinkModel link)
    {
        var placed = new List<List<Point>>();
        foreach (var other in diagram.Links)
        {
            if (!ReferenceEquals(other, link) && !SameNet(other, link) && LinkGeometry.Polyline(other) is { } polyline)
                placed.Add(polyline);
        }
        return placed;
    }

    private List<Point>? Search(List<double> xs, List<double> ys, List<Rectangle> blocks, Point s, Direction startDir,
        Point t, Direction arriveDir, List<List<Point>> placed)
    {
        static int Index(double v, List<double> axis) => axis.FindIndex(x => Math.Abs(x - v) < 1e-9);
        var (sx, sy, tx, ty) = (Index(s.X, xs), Index(s.Y, ys), Index(t.X, xs), Index(t.Y, ys));

        var best = new Dictionary<(int, int, Direction), double>();
        var from = new Dictionary<(int, int, Direction), (int, int, Direction)>();
        var queue = new PriorityQueue<(int X, int Y, Direction Dir), double>();
        var start = (sx, sy, startDir);
        best[start] = 0;
        queue.Enqueue(start, 0);

        (int, int, Direction)? goal = null;
        var goalCost = double.MaxValue;
        while (queue.TryDequeue(out var cur, out var cost))
        {
            if (cost > best[cur] || cost >= goalCost)
                continue;
            if (cur.X == tx && cur.Y == ty)
            {
                // Arriving head-on would fold back over the stub, and simplifying the polyline would erase the fold.
                if (cur.Dir == Opposite(arriveDir))
                    continue;
                var turn = cur.Dir == arriveDir ? 0 : Options.BendCost;
                if (cost + turn < goalCost)
                    (goalCost, goal) = (cost + turn, cur);
                continue;
            }

            foreach (var dir in Directions)
            {
                if (dir == Opposite(cur.Dir))
                    continue;
                var (nx, ny) = dir switch
                {
                    Direction.Left => (cur.X - 1, cur.Y),
                    Direction.Right => (cur.X + 1, cur.Y),
                    Direction.Up => (cur.X, cur.Y - 1),
                    _ => (cur.X, cur.Y + 1),
                };
                if (nx < 0 || ny < 0 || nx >= xs.Count || ny >= ys.Count)
                    continue;
                var a = new Point(xs[cur.X], ys[cur.Y]);
                var b = new Point(xs[nx], ys[ny]);
                if (blocks.Any(k => CutBy(k, a, b) || StrictlyContains(k, b)))
                    continue;
                var next = (nx, ny, dir);
                var c = cost + Length(a, b) + (dir == cur.Dir ? 0 : Options.BendCost) + Penalty(a, b, placed);
                if (best.TryGetValue(next, out var old) && old <= c)
                    continue;
                best[next] = c;
                from[next] = cur;
                queue.Enqueue(next, c);
            }
        }

        if (goal is null)
            return null;

        var path = new List<Point>();
        for (var k = goal.Value; ; k = from[k])
        {
            path.Add(new Point(xs[k.Item1], ys[k.Item2]));
            if (k == start)
                break;
        }
        path.Reverse();
        return path;
    }

    /// <summary>What it costs to lay this step next to what is already placed: crossings, running on top, hugging.</summary>
    private double Penalty(Point a, Point b, List<List<Point>> placed)
    {
        var cost = 0.0;
        var horizontal = Horizontal(a, b);
        foreach (var route in placed)
            for (var i = 0; i + 1 < route.Count; i++)
            {
                var (c, d) = (route[i], route[i + 1]);
                if (horizontal != Horizontal(c, d))
                {
                    if (CrossesStep(a, b, c, d))
                        cost += Options.CrossCost;
                    continue;
                }

                var gap = horizontal ? Math.Abs(a.Y - c.Y) : Math.Abs(a.X - c.X);
                var overlap = horizontal
                    ? Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) - Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X))
                    : Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)) - Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
                if (overlap <= 0)
                    continue;
                if (gap < Eps)
                    cost += Options.OverlapCostPerUnit * overlap;
                else if (gap < Options.Margin * 1.5)
                    cost += Options.HugCostPerUnit * overlap;
            }
        return cost;
    }

    /// <summary>
    /// Whether a grid step crosses a placed segment. Placed routes run on grid lines, so most crossings fall exactly on
    /// a step's end: the step owns the half-open range [min, max) so each crossing is paid once. Touching the other
    /// segment's end is a join, not a crossing.
    /// </summary>
    private static bool CrossesStep(Point a, Point b, Point c, Point d)
    {
        var stepHorizontal = Horizontal(a, b);
        var (along, lo, hi) = stepHorizontal
            ? (c.X, Math.Min(a.X, b.X), Math.Max(a.X, b.X))
            : (c.Y, Math.Min(a.Y, b.Y), Math.Max(a.Y, b.Y));
        var (at, otherLo, otherHi) = stepHorizontal
            ? (a.Y, Math.Min(c.Y, d.Y), Math.Max(c.Y, d.Y))
            : (a.X, Math.Min(c.X, d.X), Math.Max(c.X, d.X));
        return along >= lo - Eps && along < hi - Eps && at > otherLo + Eps && at < otherHi - Eps;
    }

    /// <summary>Drops repeated points and points in the middle of a straight run.</summary>
    private static List<Point> Simplify(IEnumerable<Point> points)
    {
        var list = new List<Point>();
        foreach (var p in points)
        {
            if (list.Count > 0 && Near(list[^1], p))
                continue;
            if (list.Count >= 2 && Collinear(list[^2], list[^1], p))
                list[^1] = p;
            else
                list.Add(p);
        }
        return list;
    }

    private static bool StrictlyContains(Rectangle r, Point p) =>
        p.X > r.Left + Eps && p.X < r.Right - Eps && p.Y > r.Top + Eps && p.Y < r.Bottom - Eps;

    /// <summary>True when an axis-parallel segment passes through the rectangle's interior.</summary>
    private static bool CutBy(Rectangle r, Point a, Point b) => Horizontal(a, b)
        ? a.Y > r.Top + Eps && a.Y < r.Bottom - Eps && Math.Max(a.X, b.X) > r.Left + Eps && Math.Min(a.X, b.X) < r.Right - Eps
        : a.X > r.Left + Eps && a.X < r.Right - Eps && Math.Max(a.Y, b.Y) > r.Top + Eps && Math.Min(a.Y, b.Y) < r.Bottom - Eps;

    private static double Length(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    private static bool Horizontal(Point a, Point b) => Math.Abs(a.Y - b.Y) < Eps;
    private static bool Near(Point a, Point b) => Math.Abs(a.X - b.X) < Eps && Math.Abs(a.Y - b.Y) < Eps;

    private static bool Collinear(Point a, Point b, Point c) =>
        (Math.Abs(a.X - b.X) < Eps && Math.Abs(b.X - c.X) < Eps) || (Math.Abs(a.Y - b.Y) < Eps && Math.Abs(b.Y - c.Y) < Eps);

    private static Direction Opposite(Direction d) => d switch
    {
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        Direction.Up => Direction.Down,
        _ => Direction.Up,
    };

    private enum Direction { Left, Right, Up, Down }

    /// <summary>Where a route ends: the line's end point, the side it leaves from, and the node it belongs to.</summary>
    private readonly record struct End(Point At, Direction Exit, Rectangle Owner);
}
