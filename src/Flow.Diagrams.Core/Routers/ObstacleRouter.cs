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
    // The search's working arrays, kept between routes; taken while in use, so a concurrent route makes its own.
    private Scratch? _scratch;

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
                    link.MarkChanged(ModelChanges.Geometry);
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
        var scratch = Interlocked.Exchange(ref _scratch, null) ?? new Scratch();
        List<Point>? path;
        try
        {
            PlacedSegments(diagram, link, scratch.Segments);
            path = Search(xs, ys, blocks, s, from.Exit, t, Opposite(to.Exit), scratch);
        }
        finally
        {
            _scratch = scratch;
        }
        // No grid path: an honest dogleg, visibly wrong, rather than nothing.
        path ??= new List<Point> { s, new(t.X, s.Y), t };

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

    /// <summary>
    /// Fills <paramref name="segments"/> with the segments of the full polylines of the other links already routed that
    /// are not of this link's net, link by link, from source to target.
    /// </summary>
    private void PlacedSegments(Diagram diagram, BaseLinkModel link, List<(Point C, Point D)> segments)
    {
        segments.Clear();
        foreach (var other in diagram.Links)
        {
            if (ReferenceEquals(other, link) || SameNet(other, link) || other.Route is not { } route)
                continue;
            // As LinkGeometry.Polyline, without building the list.
            var source = other.Source.GetPosition(other, route);
            var target = other.Target.GetPosition(other, route);
            if (source == null || target == null)
                continue;
            var previous = source;
            foreach (var point in route)
            {
                segments.Add((previous, point));
                previous = point;
            }
            segments.Add((previous, target));
        }
    }

    private List<Point>? Search(List<double> xs, List<double> ys, List<Rectangle> blocks, Point s, Direction startDir,
        Point t, Direction arriveDir, Scratch scratch)
    {
        static int Index(double v, List<double> axis) => axis.FindIndex(x => Math.Abs(x - v) < 1e-9);
        var (sx, sy, tx, ty) = (Index(s.X, xs), Index(s.Y, ys), Index(t.X, xs), Index(t.Y, ys));

        var steps = new GridSteps(xs, ys, new SegmentIndex(scratch, xs[0], ys[0], xs[^1], ys[^1], this), scratch);
        steps.Block(blocks);

        // A state is a grid point and the direction it was reached in: (y * width + x) * 4 + direction.
        var width = xs.Count;
        var from = scratch.From;
        var queue = scratch.Queue;
        queue.Clear();
        var start = GridSteps.State(sx, sy, startDir, width);
        steps.SetBest(start, 0);
        queue.Enqueue(start, 0);

        var goal = -1;
        var goalCost = double.MaxValue;
        while (queue.TryDequeue(out var cur, out var cost))
        {
            if (cost > steps.Best(cur) || cost >= goalCost)
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
                if (steps.Blocked(step, ny * width + nx))
                    continue;
                var next = GridSteps.State(nx, ny, dir, width);
                var length = Math.Abs(xs[x] - xs[nx]) + Math.Abs(ys[y] - ys[ny]);
                var c = cost + length + (dir == curDir ? 0 : Options.BendCost) + steps.Penalty(step, x, y, nx, ny);
                if (steps.Best(next) <= c)
                    continue;
                steps.SetBest(next, c);
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

    private static bool StrictlyContains(Rectangle r, Point p) => StrictlyContains(r, p.X, p.Y);

    private static bool StrictlyContains(Rectangle r, double x, double y) =>
        x > r.Left + Eps && x < r.Right - Eps && y > r.Top + Eps && y < r.Bottom - Eps;

    /// <summary>True when an axis-parallel segment passes through the rectangle's interior.</summary>
    private static bool CutBy(Rectangle r, Point a, Point b) => CutBy(r, a.X, a.Y, b.X, b.Y);

    private static bool CutBy(Rectangle r, double ax, double ay, double bx, double by) => Math.Abs(ay - by) < Eps
        ? ay > r.Top + Eps && ay < r.Bottom - Eps && Math.Max(ax, bx) > r.Left + Eps && Math.Min(ax, bx) < r.Right - Eps
        : ax > r.Left + Eps && ax < r.Right - Eps && Math.Max(ay, by) > r.Top + Eps && Math.Min(ay, by) < r.Bottom - Eps;

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
    private sealed class GridSteps
    {
        private readonly List<double> _xs;
        private readonly List<double> _ys;
        private readonly SegmentIndex _placed;
        private readonly Scratch _scratch;
        private readonly int _mark;

        public GridSteps(List<double> xs, List<double> ys, SegmentIndex placed, Scratch scratch)
        {
            (_xs, _ys, _placed, _scratch) = (xs, ys, placed, scratch);
            _mark = scratch.Begin(xs.Count * ys.Count);
        }

        /// <summary>Whether the step passes through a block's interior, or ends strictly inside one (grid point y * width + x).</summary>
        public bool Blocked(int step, int end) => _scratch.CutMark[step] == _mark || _scratch.InsideMark[end] == _mark;

        /// <summary>The search's cheapest known cost of the state; infinite until reached.</summary>
        public double Best(int state) => _scratch.BestMark[state] == _mark ? _scratch.Best[state] : double.PositiveInfinity;

        public void SetBest(int state, double cost)
        {
            _scratch.Best[state] = cost;
            _scratch.BestMark[state] = _mark;
        }

        public static int State(int x, int y, Direction dir, int width) => (y * width + x) * 4 + (int)dir;

        /// <summary>What the step from (x, y) to (nx, ny) costs next to the placed routes.</summary>
        public double Penalty(int step, int x, int y, int nx, int ny)
        {
            if (_scratch.PenaltyMark[step] == _mark)
                return _scratch.Penalty[step];
            var penalty = _placed.Penalty(new Point(_xs[x], _ys[y]), new Point(_xs[nx], _ys[ny]));
            _scratch.Penalty[step] = penalty;
            _scratch.PenaltyMark[step] = _mark;
            return penalty;
        }

        public void Block(List<Rectangle> blocks)
        {
            var width = _xs.Count;
            foreach (var block in blocks)
            {
                // Only the points and steps that touch the block can be inside it or cut it.
                var (x0, x1) = Points(_xs, block.Left - 1, block.Right + 1);
                var (y0, y1) = Points(_ys, block.Top - 1, block.Bottom + 1);
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    if (StrictlyContains(block, _xs[x], _ys[y]))
                        _scratch.InsideMark[y * width + x] = _mark;

                var (sx0, sx1) = (Math.Max(x0 - 1, 0), Math.Min(x1, width - 2));
                var (sy0, sy1) = (Math.Max(y0 - 1, 0), Math.Min(y1, _ys.Count - 2));
                for (var y = y0; y <= y1; y++)
                for (var x = sx0; x <= sx1; x++)
                {
                    var (left, right, at) = (_xs[x], _xs[x + 1], _ys[y]);
                    if (CutBy(block, left, at, right, at))
                        _scratch.CutMark[State(x, y, Direction.Right, width)] = _mark;
                    if (CutBy(block, right, at, left, at))
                        _scratch.CutMark[State(x + 1, y, Direction.Left, width)] = _mark;
                }
                for (var y = sy0; y <= sy1; y++)
                for (var x = x0; x <= x1; x++)
                {
                    var (at, top, bottom) = (_xs[x], _ys[y], _ys[y + 1]);
                    if (CutBy(block, at, top, at, bottom))
                        _scratch.CutMark[State(x, y, Direction.Down, width)] = _mark;
                    if (CutBy(block, at, bottom, at, top))
                        _scratch.CutMark[State(x, y + 1, Direction.Up, width)] = _mark;
                }
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
    /// The placed segments bucketed on a coarse grid, so that a step is priced against the segments near it only.
    /// Those are summed in the order they were placed, as when every segment was visited, so the total is the same to
    /// the last bit.
    /// </summary>
    private sealed class SegmentIndex
    {
        private const int MaxColumns = 128;

        private readonly ObstacleRouter _router;
        private readonly List<(Point C, Point D)> _segments;
        private readonly double _left, _top, _cellWidth, _cellHeight;
        private readonly int _columns, _rows;
        // The segments of cell k are _entries[_cellStart[k] .. _cellStart[k + 1]), in placing order.
        private readonly int[] _cellStart;
        private readonly int[] _entries;
        private readonly int[] _seen;
        private readonly List<int> _near;
        private int _query;

        /// <param name="scratch">Holds the placed segments, and the arrays the index is built in.</param>
        /// <param name="left">Left of the area the steps lie in.</param>
        /// <param name="top">Top of that area.</param>
        /// <param name="right">Right of that area.</param>
        /// <param name="bottom">Bottom of that area.</param>
        /// <param name="router">The router whose costs price a step.</param>
        public SegmentIndex(Scratch scratch, double left, double top, double right, double bottom, ObstacleRouter router)
        {
            (_router, _segments, _near) = (router, scratch.Segments, scratch.Near);
            _columns = _rows = Math.Clamp((int)Math.Sqrt(_segments.Count), 1, MaxColumns);
            (_left, _top) = (left, top);
            _cellWidth = Math.Max((right - left) / _columns, Eps);
            _cellHeight = Math.Max((bottom - top) / _rows, Eps);
            var cells = _columns * _rows;
            _cellStart = Scratch.Cleared(ref scratch.CellStart, cells + 1, 0);
            _seen = Scratch.Cleared(ref scratch.Seen, _segments.Count, 0);

            // A segment costs a step nothing unless the step comes within the hugging distance of its box. Counted
            // first, then filled, so that every cell lists its segments in placing order without a list of its own.
            var reach = Math.Max(router.Options.Margin * 1.5, 0) + 1;
            var total = 0;
            for (var i = 0; i < _segments.Count; i++)
            {
                var (c0, c1, r0, r1) = Cells(i, reach);
                for (var row = r0; row <= r1; row++)
                for (var column = c0; column <= c1; column++)
                    _cellStart[row * _columns + column + 1]++;
                total += (c1 - c0 + 1) * (r1 - r0 + 1);
            }
            for (var k = 0; k < cells; k++)
                _cellStart[k + 1] += _cellStart[k];

            _entries = Scratch.Sized(ref scratch.Entries, total);
            var fill = Scratch.Sized(ref scratch.Fill, cells);
            Array.Copy(_cellStart, fill, cells);
            for (var i = 0; i < _segments.Count; i++)
            {
                var (c0, c1, r0, r1) = Cells(i, reach);
                for (var row = r0; row <= r1; row++)
                for (var column = c0; column <= c1; column++)
                    _entries[fill[row * _columns + column]++] = i;
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
                var cell = row * _columns + column;
                for (var k = _cellStart[cell]; k < _cellStart[cell + 1]; k++)
                {
                    var i = _entries[k];
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

        private (int C0, int C1, int R0, int R1) Cells(int segment, double reach)
        {
            var (c, d) = _segments[segment];
            return (Column(Math.Min(c.X, d.X) - reach), Column(Math.Max(c.X, d.X) + reach),
                Row(Math.Min(c.Y, d.Y) - reach), Row(Math.Max(c.Y, d.Y) + reach));
        }

        // Clamped in floating point first, so that coordinates far outside the area cannot overflow the cast.
        private int Column(double x) => (int)Math.Clamp(Math.Floor((x - _left) / _cellWidth), 0, _columns - 1);
        private int Row(double y) => (int)Math.Clamp(Math.Floor((y - _top) / _cellHeight), 0, _rows - 1);
    }

    /// <summary>
    /// Working storage of one search, reused by the next; the arrays grow to the largest grid routed so far. An entry
    /// of a marked array holds for the current search only when its mark is the search's, so nothing is cleared
    /// between searches.
    /// </summary>
    private sealed class Scratch
    {
        private int _mark;

        public readonly List<(Point C, Point D)> Segments = new();
        public readonly List<int> Near = new();
        public readonly PriorityQueue<int, double> Queue = new();
        public int[] CutMark = Array.Empty<int>();
        public int[] InsideMark = Array.Empty<int>();
        public double[] Penalty = Array.Empty<double>();
        public int[] PenaltyMark = Array.Empty<int>();
        public double[] Best = Array.Empty<double>();
        public int[] BestMark = Array.Empty<int>();
        public int[] From = Array.Empty<int>();
        public int[] CellStart = Array.Empty<int>();
        public int[] Entries = Array.Empty<int>();
        public int[] Fill = Array.Empty<int>();
        public int[] Seen = Array.Empty<int>();

        /// <summary>Sizes the grid arrays for <paramref name="points"/> grid points and returns the new search's mark.</summary>
        public int Begin(int points)
        {
            var states = points * 4;
            Sized(ref InsideMark, points);
            Sized(ref CutMark, states);
            Sized(ref PenaltyMark, states);
            Sized(ref BestMark, states);
            Sized(ref Penalty, states);
            Sized(ref Best, states);
            Sized(ref From, states);
            if (_mark == int.MaxValue)
            {
                // The marks are about to wrap: clearing them makes every entry stale again.
                Array.Clear(InsideMark);
                Array.Clear(CutMark);
                Array.Clear(PenaltyMark);
                Array.Clear(BestMark);
                _mark = 0;
            }
            return ++_mark;
        }

        /// <summary>The array, grown to at least <paramref name="length"/>; its contents are left as they are.</summary>
        public static T[] Sized<T>(ref T[] array, int length)
        {
            if (array.Length < length)
                array = new T[Math.Max(length, array.Length * 2)];
            return array;
        }

        /// <summary>The array, grown to at least <paramref name="length"/>, with its first <paramref name="length"/> entries set to <paramref name="value"/>.</summary>
        public static T[] Cleared<T>(ref T[] array, int length, T value)
        {
            Array.Fill(Sized(ref array, length), value, 0, length);
            return array;
        }
    }

    /// <summary>Where a route ends: the line's end point, the side it leaves from, and the node it belongs to.</summary>
    private readonly record struct End(Point At, Direction Exit, Rectangle Owner);
}
