using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.PathGenerators;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Keeps <see cref="BaseLinkModel.Halos"/> up to date: wherever a link crosses a link of another net drawn below it
/// (see <see cref="Diagram.OrderedSelectables"/>), it gets a halo that breaks the lower link there. Halos follow the
/// route's straight segments, so only links drawn with <see cref="StraightPathGenerator"/> get them. Not registered by
/// default: <c>diagram.RegisterBehavior(new LinkHalosBehavior(diagram))</c>.
/// </summary>
/// <remarks>
/// Setting a link's <see cref="BaseLinkModel.PathGenerator"/>, or the diagram's default one, raises no event: the
/// halos follow the switch at the link's next <see cref="Model.Refresh"/>, as its drawing does.
/// <para>When a link's route changes, only the links whose bounding boxes overlap its old or its new one are recomputed, so
/// the cost of a change follows the links around it rather than the size of the diagram.</para>
/// </remarks>
public class LinkHalosBehavior : Behavior
{
    private readonly Dictionary<BaseLinkModel, Tracked> _tracked = new();
    // Every tracked link in drawing order; Tracked.Rank is the index here.
    private List<Tracked> _order = new();
    // Whether two links share a layer and an order, which leaves their place to the diagram's list.
    private bool _hasTies;

    /// <summary>Computes the halos of the links already in <paramref name="diagram"/> and keeps them current from then on.</summary>
    public LinkHalosBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.Links.Added += OnLinkAdded;
        Diagram.Links.Removed += OnLinkRemoved;
        Diagram.Changed += OnDiagramChanged;
        foreach (var link in Diagram.Links)
            Track(link);
        SetOrder(DrawingOrder());
        UpdateAll();
    }

    /// <summary>Gap between the lower link's stroke and the halo's ends, and around the upper link's stroke.</summary>
    public double Clearance { get; set; } = 2;

    /// <summary>Whether two links never break each other. By default, links that share a port.</summary>
    protected virtual bool SameNet(BaseLinkModel a, BaseLinkModel b) => LinkGeometry.SharePort(a, b);

    /// <summary>Stops tracking links; halos already set stay on the links.</summary>
    public override void Dispose()
    {
        Diagram.Links.Added -= OnLinkAdded;
        Diagram.Links.Removed -= OnLinkRemoved;
        Diagram.Changed -= OnDiagramChanged;
        foreach (var link in _tracked.Keys)
        {
            link.Changed -= OnLinkChanged;
            link.OrderChanged -= OnLinkOrderChanged;
            link.VisibilityChanged -= OnLinkVisibilityChanged;
        }
        _tracked.Clear();
        _order.Clear();
    }

    private Tracked Track(BaseLinkModel link)
    {
        var tracked = new Tracked(link) { Straight = Straight(link) };
        tracked.SetPolyline(LinkGeometry.Polyline(link));
        _tracked[link] = tracked;
        link.Changed += OnLinkChanged;
        link.OrderChanged += OnLinkOrderChanged;
        link.VisibilityChanged += OnLinkVisibilityChanged;
        return tracked;
    }

    private void OnLinkAdded(BaseLinkModel link)
    {
        var tracked = Track(link);
        // A new link is usually drawn on top of every other; anywhere else, the order is rebuilt.
        if (_order.Count == 0 || CompareDrawingOrder(_order[^1].Link, link) < 0)
        {
            tracked.Rank = _order.Count;
            _order.Add(tracked);
        }
        else
        {
            SetOrder(DrawingOrder());
        }
        UpdateAround(tracked, Box.Empty);
    }

    private void OnLinkRemoved(BaseLinkModel link)
    {
        link.Changed -= OnLinkChanged;
        link.OrderChanged -= OnLinkOrderChanged;
        link.VisibilityChanged -= OnLinkVisibilityChanged;
        if (!_tracked.Remove(link, out var removed))
            return;

        _order.RemoveAt(removed.Rank);
        Renumber(removed.Rank);
        // Only the links drawn above it had halos for it.
        for (var rank = removed.Rank; rank < _order.Count; rank++)
            if (_order[rank].Box.Overlaps(removed.Box))
                Recompute(_order[rank]);
    }

    private void OnLinkChanged(Model model, ModelChanges changes)
    {
        if (!changes.HasFlag(ModelChanges.Geometry))
            return;

        if (!_tracked.TryGetValue((BaseLinkModel)model, out var tracked))
            return;

        // The same polyline drawn the same way means only the halos or the looks changed, which moves no crossing.
        var polyline = LinkGeometry.Polyline(tracked.Link);
        var straight = Straight(tracked.Link);
        if (SamePolyline(tracked.Polyline, polyline) && straight == tracked.Straight)
            return;

        var oldBox = tracked.Box;
        tracked.SetPolyline(polyline);
        tracked.Straight = straight;
        UpdateAround(tracked, oldBox);
    }

    // A hidden link breaks nothing and keeps the halos it had; shown again, it gets its own and breaks those above it.
    private void OnLinkVisibilityChanged(Model model)
    {
        if (!_tracked.TryGetValue((BaseLinkModel)model, out var tracked))
            return;

        if (tracked.Link.Visible)
            Recompute(tracked);
        RecomputeAbove(tracked, tracked.Box);
    }

    /// <summary>
    /// Links tied in layer and order are drawn in the diagram's list order, which its sort, not a stable one, may
    /// permute without an event on any link (a node's order changing re-sorts the list). Every re-sort refreshes the
    /// diagram, so the order is read again then, when there are ties.
    /// </summary>
    private void OnDiagramChanged()
    {
        if (!_hasTies)
            return;

        var order = DrawingOrder();
        if (order.SequenceEqual(_order))
            return;
        SetOrder(order);
        UpdateAll();
    }

    private void OnLinkOrderChanged(SelectableModel model)
    {
        if (!_tracked.TryGetValue((BaseLinkModel)model, out var moved))
            return;

        var oldOrder = _order;
        var oldRank = moved.Rank;
        SetOrder(DrawingOrder());
        if (!SameExcept(oldOrder, _order, moved))
        {
            UpdateAll();
            return;
        }
        if (moved.Rank == oldRank)
            return;

        // Only the moved link changed places, so only the pairs that include it can have changed sides.
        Recompute(moved);
        foreach (var other in _order)
            if (other != moved && other.Box.Overlaps(moved.Box))
                Recompute(other);
    }

    /// <summary>Whether the two orders list the same links in the same sequence once <paramref name="moved"/> is left out.</summary>
    private static bool SameExcept(List<Tracked> a, List<Tracked> b, Tracked moved)
    {
        if (a.Count != b.Count)
            return false;
        int i = 0, j = 0;
        while (true)
        {
            while (i < a.Count && a[i] == moved) i++;
            while (j < b.Count && b[j] == moved) j++;
            if (i == a.Count || j == b.Count)
                return i == a.Count && j == b.Count;
            if (a[i] != b[j])
                return false;
            i++;
            j++;
        }
    }

    private static bool SamePolyline(List<Point>? a, List<Point>? b) =>
        a == null ? b == null : b != null && a.SequenceEqual(b);

    /// <summary>
    /// The tracked links by layer, then order, ties kept in <see cref="Diagram.OrderedSelectables"/> order. Sorted
    /// here: SendToFront and SendToBack raise OrderChanged before the diagram re-sorts its list.
    /// </summary>
    private List<Tracked> DrawingOrder()
    {
        var order = new List<Tracked>(_tracked.Count);
        foreach (var selectable in Diagram.OrderedSelectables)
            if (selectable is BaseLinkModel link && _tracked.TryGetValue(link, out var tracked))
                order.Add(tracked);
        return order.OrderBy(t => t.Link.Layer).ThenBy(t => t.Link.Order).ToList();
    }

    private static int CompareDrawingOrder(BaseLinkModel a, BaseLinkModel b)
    {
        var byLayer = a.Layer.CompareTo(b.Layer);
        return byLayer != 0 ? byLayer : a.Order.CompareTo(b.Order);
    }

    private void SetOrder(List<Tracked> order)
    {
        _order = order;
        Renumber(0);
        _hasTies = false;
        for (var rank = 1; rank < _order.Count && !_hasTies; rank++)
            _hasTies = CompareDrawingOrder(_order[rank - 1].Link, _order[rank].Link) == 0;
    }

    private void Renumber(int from)
    {
        for (var rank = from; rank < _order.Count; rank++)
            _order[rank].Rank = rank;
    }

    private void UpdateAll()
    {
        foreach (var tracked in _order)
            Recompute(tracked);
    }

    /// <summary>
    /// Recomputes <paramref name="changed"/> and the links drawn above it that overlap where it was or where it is: no
    /// other link's halos involve it.
    /// </summary>
    private void UpdateAround(Tracked changed, Box oldBox)
    {
        if (!changed.Link.Visible)
            return;

        Recompute(changed);
        RecomputeAbove(changed, oldBox);
    }

    /// <summary>Recomputes the links drawn above <paramref name="changed"/> that overlap where it is or <paramref name="oldBox"/>.</summary>
    private void RecomputeAbove(Tracked changed, Box oldBox)
    {
        for (var rank = changed.Rank + 1; rank < _order.Count; rank++)
        {
            var upper = _order[rank];
            if (upper.Box.Overlaps(changed.Box) || upper.Box.Overlaps(oldBox))
                Recompute(upper);
        }
    }

    /// <summary>The halos of <paramref name="upper"/> against every visible link drawn below it; a hidden link is left as it is.</summary>
    private void Recompute(Tracked upper)
    {
        var link = upper.Link;
        if (!link.Visible)
            return;
        if (upper.Polyline is not { } over || !Straight(link))
        {
            link.SetHalos(Array.Empty<LinkHalo>());
            return;
        }

        List<LinkHalo>? halos = null;
        for (var rank = 0; rank < upper.Rank; rank++)
        {
            var lower = _order[rank];
            // Two polylines can only cross where their boxes meet.
            if (!lower.Box.Overlaps(upper.Box) || lower.Polyline is not { } under || !lower.Link.Visible
                || !Straight(lower.Link) || SameNet(link, lower.Link))
                continue;

            for (var i = 0; i + 1 < over.Count; i++)
            for (var j = 0; j + 1 < under.Count; j++)
            {
                var (a, b, c, d) = (over[i], over[i + 1], under[j], under[j + 1]);
                if (LinkGeometry.Crossing(a, b, c, d) is { } at)
                    (halos ??= new List<LinkHalo>()).Add(Halo(at, a, b, d - c, StrokeWidth(link), StrokeWidth(lower.Link)));
            }
        }
        link.SetHalos(halos ?? (IReadOnlyList<LinkHalo>)Array.Empty<LinkHalo>());
    }

    // Only LinkModel has a width; other links are drawn by their own widgets and assumed at LinkModel's default, 2.
    private static double StrokeWidth(BaseLinkModel link) => link is LinkModel l ? l.Width : 2;

    private bool Straight(BaseLinkModel link) =>
        (link.PathGenerator ?? Diagram.Options.Links.DefaultPathGenerator) is StraightPathGenerator;

    /// <summary>
    /// The halo at <paramref name="at"/>, laid along the upper segment from <paramref name="a"/> to <paramref
    /// name="b"/>: long enough to clear the lower stroke, running along <paramref name="under"/>, by the clearance on
    /// each side, which takes longer the shallower the crossing.
    /// </summary>
    private LinkHalo Halo(Point at, Point a, Point b, Point under, double overWidth, double underWidth)
    {
        var half = underWidth / 2 + Clearance;
        var width = overWidth + 2 * Clearance;
        if (LinkGeometry.Horizontal(a, b) && Math.Abs(under.X) < 1e-6)
            return new LinkHalo(new Point(at.X - half, at.Y), new Point(at.X + half, at.Y), width);
        if (Math.Abs(a.X - b.X) < 1e-6 && Math.Abs(under.Y) < 1e-6)
            return new LinkHalo(new Point(at.X, at.Y - half), new Point(at.X, at.Y + half), width);

        var along = (b - a).Normalize();
        // Below about 15 degrees the halo would erase a long run of the lower link; it stays at four times its square length.
        var sine = Math.Max(Math.Abs(LinkGeometry.Cross(along, under.Normalize())), 0.25);
        var offset = along.Multiply(half / sine);
        return new LinkHalo(at - offset, at + offset, width);
    }

    /// <summary>
    /// A link with the polyline its halos were last computed from, that polyline's box, whether it was drawn straight,
    /// and its place in the drawing order.
    /// </summary>
    private sealed class Tracked
    {
        public Tracked(BaseLinkModel link) => Link = link;

        public BaseLinkModel Link { get; }
        public List<Point>? Polyline { get; private set; }
        public Box Box { get; private set; } = Box.Empty;
        public int Rank { get; set; }
        public bool Straight { get; set; }

        public void SetPolyline(List<Point>? polyline)
        {
            Polyline = polyline;
            Box = polyline == null ? Box.Empty : Box.Of(polyline);
        }
    }

    /// <summary>A closed axis-aligned box; <see cref="Empty"/> overlaps nothing.</summary>
    private readonly record struct Box(double Left, double Top, double Right, double Bottom)
    {
        public static Box Empty => new(double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);

        public static Box Of(List<Point> points)
        {
            var box = Empty;
            foreach (var p in points)
                box = new Box(Math.Min(box.Left, p.X), Math.Min(box.Top, p.Y), Math.Max(box.Right, p.X), Math.Max(box.Bottom, p.Y));
            return box;
        }

        public bool Overlaps(Box other) =>
            Left <= other.Right && other.Left <= Right && Top <= other.Bottom && other.Top <= Bottom;
    }
}
