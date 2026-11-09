using System.Buffers;
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
    /// <remarks>Vertices are ignored, so <c>false</c>.</remarks>
    public override bool FollowsVertices => false;

    /// <inheritdoc />
    /// <remarks>Returns an empty route when the two ports face each other on one line with nothing in between.</remarks>
    public override Point[] GetRoute(Diagram diagram, BaseLinkModel link)
    {
        if (ResolveEnd(link, link.Source) is not { } from || ResolveEnd(link, link.Target) is not { } to)
            return _fallbackRouter.GetRoute(diagram, link);

        var obstacles = GetObstacles(diagram, link).ToList();
        if (Facing(from, to) && !obstacles.Any(o => !o.Equals(from.Owner) && !o.Equals(to.Owner) && CutBy(o, from.At, to.At)))
            return Array.Empty<Point>();

        var s = StubEnd(from, link.SourceMarker != null ? Options.MinStub : 1);
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

        using var steps = new GridSteps(xs, ys, new PlacedSegments(placed, xs[0], ys[0], xs[^1], ys[^1], this));
        steps.Block(blocks);

        // A state is a grid point and the direction it was reached in: (y * width + x) * 4 + direction.
        var width = xs.Count;
        var (best, from) = (steps.Best, steps.From);
        var queue = new PriorityQueue<int, double>();
        var start = GridSteps.State(sx, sy, startDir, width);
        best[start] = 0;
        queue.Enqueue(start, 0);

        var goal = -1;
        var goalCost = double.MaxValue;
        while (queue.TryDequeue(out var cur, out var cost))
        {
            if (cost > best[cur] || cost >= goalCost)
                continue;
            var (x, y, curDir) = (cur / 4 % width, cur / 4 / width, (Direction)(cur % 4));
            if (x == tx && y == ty)
            {
                // Arriving head-on would fold back over the stub, and simplifying the polyline would erase the fold.
                if (curDir == Opposite(arriveDir))
                    continue;
                var turn = curDir == arriveDir ? 0 : Options.BendCost;
                if (cost + turn < goalCost)
                    (goalCost, goal) = (cost + turn, cur);
                continue;
            }

            foreach (var dir in Directions)
            {
                if (dir == Opposite(curDir))
                    continue;
                var (nx, ny) = Step(x, y, dir);
                if (nx < 0 || ny < 0 || nx >= xs.Count || ny >= ys.Count)
                    continue;
                var step = GridSteps.State(x, y, dir, width);
                if (steps.Cut[step] || steps.Inside[ny * width + nx])
                    continue;
                var next = GridSteps.State(nx, ny, dir, width);
                var length = Math.Abs(xs[x] - xs[nx]) + Math.Abs(ys[y] - ys[ny]);
                var c = cost + length + (dir == curDir ? 0 : Options.BendCost) + steps.Penalty(step, x, y, nx, ny);
                if (best[next] <= c)
                    continue;
                best[next] = c;
                from[next] = cur;
                queue.Enqueue(next, c);
            }
        }

        if (goal < 0)
            return null;

        var path = new List<Point>();
        for (var k = goal; ; k = from[k])
        {
            path.Add(new Point(xs[k / 4 % width], ys[k / 4 / width]));
            if (k == start)
                break;
        }
        path.Reverse();
        return path;
    }

    private static (int X, int Y) Step(int x, int y, Direction dir) => dir switch
    {
        Direction.Left => (x - 1, y),
        Direction.Right => (x + 1, y),
        Direction.Up => (x, y - 1),
        _ => (x, y + 1),
    };

    /// <summary>
    /// What it costs to lay the step from <paramref name="a"/> to <paramref name="b"/> next to the placed segment from
    /// <paramref name="c"/> to <paramref name="d"/>: crossing it, running on top of it or hugging it. False when the
    /// segment costs the step nothing.
    /// </summary>
    private bool Penalty(Point a, Point b, Point c, Point d, out double cost)
    {
        cost = 0;
        var horizontal = Horizontal(a, b);
        if (horizontal != Horizontal(c, d))
        {
            if (!CrossesStep(a, b, c, d))
                return false;
            cost = Options.CrossCost;
            return true;
        }

        var gap = horizontal ? Math.Abs(a.Y - c.Y) : Math.Abs(a.X - c.X);
        var overlap = horizontal
            ? Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) - Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X))
            : Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)) - Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
        if (overlap <= 0)
            return false;
        if (gap < Eps)
            cost = Options.OverlapCostPerUnit * overlap;
        else if (gap < Options.Margin * 1.5)
            cost = Options.HugCostPerUnit * overlap;
        else
            return false;
        return true;
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

    /// <summary>
    /// What the steps of one search's grid cost. Whether a step is blocked is worked out up front from the blocks
    /// near it; what it costs next to the placed routes, the first time the search takes it. A step is the grid point
    /// it leaves and its direction, indexed like a search state.
    /// </summary>
    private sealed class GridSteps : IDisposable
    {
        private readonly List<double> _xs;
        private readonly List<double> _ys;
        private readonly PlacedSegments _placed;
        private readonly double[] _penalty;

        // Rented, since a grid of a few thousand points would put every array on the large object heap; a rented array
        // may be longer than asked, and only the first States or Points entries are used.
        public GridSteps(List<double> xs, List<double> ys, PlacedSegments placed)
        {
            (_xs, _ys, _placed) = (xs, ys, placed);
            var points = xs.Count * ys.Count;
            var states = points * 4;
            Cut = ArrayPool<bool>.Shared.Rent(states);
            Array.Clear(Cut, 0, states);
            Inside = ArrayPool<bool>.Shared.Rent(points);
            Array.Clear(Inside, 0, points);
            _penalty = ArrayPool<double>.Shared.Rent(states);
            Array.Fill(_penalty, double.NaN, 0, states);
            Best = ArrayPool<double>.Shared.Rent(states);
            Array.Fill(Best, double.PositiveInfinity, 0, states);
            From = ArrayPool<int>.Shared.Rent(states);
        }

        /// <summary>Whether the step passes through a block's interior.</summary>
        public bool[] Cut { get; }

        /// <summary>Whether the grid point (y * width + x) is strictly inside a block.</summary>
        public bool[] Inside { get; }

        /// <summary>The search's cheapest known cost of each state; infinite until reached.</summary>
        public double[] Best { get; }

        /// <summary>The state each reached state was reached from.</summary>
        public int[] From { get; }

        public void Dispose()
        {
            ArrayPool<bool>.Shared.Return(Cut);
            ArrayPool<bool>.Shared.Return(Inside);
            ArrayPool<double>.Shared.Return(_penalty);
            ArrayPool<double>.Shared.Return(Best);
            ArrayPool<int>.Shared.Return(From);
        }

        public static int State(int x, int y, Direction dir, int width) => (y * width + x) * 4 + (int)dir;

        /// <summary>What the step from (x, y) to (nx, ny) costs next to the placed routes.</summary>
        public double Penalty(int step, int x, int y, int nx, int ny)
        {
            var penalty = _penalty[step];
            if (double.IsNaN(penalty))
                _penalty[step] = penalty = _placed.Penalty(new Point(_xs[x], _ys[y]), new Point(_xs[nx], _ys[ny]));
            return penalty;
        }

        public void Block(List<Rectangle> blocks)
        {
            foreach (var block in blocks)
            {
                // Only the points and steps that touch the block can be inside it or cut it.
                var (x0, x1) = Points(_xs, block.Left - 1, block.Right + 1);
                var (y0, y1) = Points(_ys, block.Top - 1, block.Bottom + 1);
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    Inside[y * _xs.Count + x] |= StrictlyContains(block, new Point(_xs[x], _ys[y]));

                ForEachStep(block.Left - 1, block.Top - 1, block.Right + 1, block.Bottom + 1,
                    (step, a, b) => Cut[step] |= CutBy(block, a, b));
            }
        }

        /// <summary>Calls <paramref name="visit"/> for every step, in both directions, whose span meets the box.</summary>
        private void ForEachStep(double left, double top, double right, double bottom, Action<int, Point, Point> visit)
        {
            var width = _xs.Count;
            var (x0, x1) = Points(_xs, left, right);
            var (y0, y1) = Points(_ys, top, bottom);
            var (sx0, sx1) = (Math.Max(x0 - 1, 0), Math.Min(x1, width - 2));
            var (sy0, sy1) = (Math.Max(y0 - 1, 0), Math.Min(y1, _ys.Count - 2));
            for (var y = y0; y <= y1; y++)
            for (var x = sx0; x <= sx1; x++)
            {
                var (a, b) = (new Point(_xs[x], _ys[y]), new Point(_xs[x + 1], _ys[y]));
                visit(State(x, y, Direction.Right, width), a, b);
                visit(State(x + 1, y, Direction.Left, width), b, a);
            }
            for (var y = sy0; y <= sy1; y++)
            for (var x = x0; x <= x1; x++)
            {
                var (a, b) = (new Point(_xs[x], _ys[y]), new Point(_xs[x], _ys[y + 1]));
                visit(State(x, y, Direction.Down, width), a, b);
                visit(State(x, y + 1, Direction.Up, width), b, a);
            }
        }

        /// <summary>The first and last index of the sorted axis whose value lies in [lo, hi]; first > last when none does.</summary>
        private static (int First, int Last) Points(List<double> axis, double lo, double hi)
        {
            var first = LowerBound(axis, lo);
            var last = LowerBound(axis, hi);
            if (last == axis.Count || axis[last] > hi)
                last--;
            return (first, last);
        }

        private static int LowerBound(List<double> axis, double value)
        {
            int lo = 0, hi = axis.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (axis[mid] < value)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }
    }

    /// <summary>
    /// The segments of the placed routes, bucketed on a coarse grid so that a step is priced against the segments near
    /// it only. Those are summed in the order they were placed, as when every segment was visited, so the total is the
    /// same to the last bit.
    /// </summary>
    private sealed class PlacedSegments
    {
        private const int MaxColumns = 128;

        private readonly ObstacleRouter _router;
        private readonly List<(Point C, Point D)> _segments = new();
        private readonly List<int>?[] _cells;
        private readonly double _left, _top, _cellWidth, _cellHeight;
        private readonly int _columns, _rows;
        private readonly int[] _seen;
        private readonly List<int> _near = new();
        private int _query;

        /// <param name="placed">The placed routes.</param>
        /// <param name="left">Left of the area the steps lie in.</param>
        /// <param name="top">Top of that area.</param>
        /// <param name="right">Right of that area.</param>
        /// <param name="bottom">Bottom of that area.</param>
        /// <param name="router">The router whose costs price a step.</param>
        public PlacedSegments(List<List<Point>> placed, double left, double top, double right, double bottom, ObstacleRouter router)
        {
            _router = router;
            foreach (var route in placed)
                for (var i = 0; i + 1 < route.Count; i++)
                    _segments.Add((route[i], route[i + 1]));

            _columns = _rows = Math.Clamp((int)Math.Sqrt(_segments.Count), 1, MaxColumns);
            (_left, _top) = (left, top);
            _cellWidth = Math.Max((right - left) / _columns, Eps);
            _cellHeight = Math.Max((bottom - top) / _rows, Eps);
            _cells = new List<int>?[_columns * _rows];
            _seen = new int[_segments.Count];

            // A segment costs a step nothing unless the step comes within the hugging distance of its box.
            var reach = Math.Max(router.Options.Margin * 1.5, 0) + 1;
            for (var i = 0; i < _segments.Count; i++)
            {
                var (c, d) = _segments[i];
                var (c0, c1) = (Column(Math.Min(c.X, d.X) - reach), Column(Math.Max(c.X, d.X) + reach));
                var (r0, r1) = (Row(Math.Min(c.Y, d.Y) - reach), Row(Math.Max(c.Y, d.Y) + reach));
                for (var row = r0; row <= r1; row++)
                for (var column = c0; column <= c1; column++)
                    (_cells[row * _columns + column] ??= new List<int>()).Add(i);
            }
        }

        /// <summary>What the step from <paramref name="a"/> to <paramref name="b"/> costs next to every placed segment.</summary>
        public double Penalty(Point a, Point b)
        {
            _query++;
            _near.Clear();
            var (c0, c1) = (Column(Math.Min(a.X, b.X)), Column(Math.Max(a.X, b.X)));
            var (r0, r1) = (Row(Math.Min(a.Y, b.Y)), Row(Math.Max(a.Y, b.Y)));
            for (var row = r0; row <= r1; row++)
            for (var column = c0; column <= c1; column++)
            {
                if (_cells[row * _columns + column] is not { } cell)
                    continue;
                foreach (var i in cell)
                {
                    if (_seen[i] == _query)
                        continue;
                    _seen[i] = _query;
                    _near.Add(i);
                }
            }

            _near.Sort();
            var cost = 0.0;
            foreach (var i in _near)
                if (_router.Penalty(a, b, _segments[i].C, _segments[i].D, out var term))
                    cost += term;
            return cost;
        }

        // Clamped in floating point first, so that coordinates far outside the area cannot overflow the cast.
        private int Column(double x) => (int)Math.Clamp(Math.Floor((x - _left) / _cellWidth), 0, _columns - 1);
        private int Row(double y) => (int)Math.Clamp(Math.Floor((y - _top) / _cellHeight), 0, _rows - 1);
    }

    /// <summary>Where a route ends: the line's end point, the side it leaves from, and the node it belongs to.</summary>
    private readonly record struct End(Point At, Direction Exit, Rectangle Owner);
}
