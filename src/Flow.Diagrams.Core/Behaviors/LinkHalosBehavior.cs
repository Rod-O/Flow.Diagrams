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
public class LinkHalosBehavior : Behavior
{
    private readonly Dictionary<BaseLinkModel, List<Point>?> _polylines = new();

    /// <summary>Computes the halos of the links already in <paramref name="diagram"/> and keeps them current from then on.</summary>
    public LinkHalosBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.Links.Added += OnLinkAdded;
        Diagram.Links.Removed += OnLinkRemoved;
        foreach (var link in Diagram.Links)
            OnLinkAdded(link);
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
        foreach (var link in _polylines.Keys)
        {
            link.Changed -= OnLinkChanged;
            link.OrderChanged -= OnLinkOrderChanged;
        }
        _polylines.Clear();
    }

    private void OnLinkAdded(BaseLinkModel link)
    {
        _polylines[link] = LinkGeometry.Polyline(link);
        link.Changed += OnLinkChanged;
        link.OrderChanged += OnLinkOrderChanged;
        UpdateFrom(link);
    }

    private void OnLinkRemoved(BaseLinkModel link)
    {
        link.Changed -= OnLinkChanged;
        link.OrderChanged -= OnLinkOrderChanged;
        _polylines.Remove(link);
        UpdateAll();
    }

    private void OnLinkChanged(Model model)
    {
        var link = (BaseLinkModel)model;
        // The same polyline means only the halos or the looks changed, which moves no crossing.
        var polyline = LinkGeometry.Polyline(link);
        if (_polylines.TryGetValue(link, out var known) && SamePolyline(known, polyline))
            return;

        _polylines[link] = polyline;
        UpdateFrom(link);
    }

    private void OnLinkOrderChanged(SelectableModel _) => UpdateAll();

    private static bool SamePolyline(List<Point>? a, List<Point>? b) =>
        a == null ? b == null : b != null && a.SequenceEqual(b);

    private void UpdateAll() => UpdateFrom(null);

    /// <summary>Recomputes the halos of <paramref name="changed"/> and of every link drawn above it (all links when null).</summary>
    private void UpdateFrom(BaseLinkModel? changed)
    {
        var drawn = Diagram.OrderedSelectables.OfType<BaseLinkModel>().Where(l => l.Visible).ToList();
        var start = changed == null ? 0 : drawn.IndexOf(changed);
        if (start < 0)
            return;

        var polylines = drawn.Select(l => Straight(l) ? LinkGeometry.Polyline(l) : null).ToList();
        for (var upper = start; upper < drawn.Count; upper++)
        {
            var link = drawn[upper];
            if (polylines[upper] is not { } over)
            {
                link.SetHalos(Array.Empty<LinkHalo>());
                continue;
            }

            var halos = new List<LinkHalo>();
            for (var lower = 0; lower < upper; lower++)
            {
                if (polylines[lower] is not { } under || SameNet(link, drawn[lower]))
                    continue;
                foreach (var (a, b) in LinkGeometry.Segments(over))
                foreach (var (c, d) in LinkGeometry.Segments(under))
                    if (LinkGeometry.Crossing(a, b, c, d) is { } at)
                        halos.Add(Halo(at, a, b, d - c, StrokeWidth(link), StrokeWidth(drawn[lower])));
            }
            link.SetHalos(halos);
        }
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
        var sine = Math.Abs(LinkGeometry.Cross(along, under.Normalize()));
        var offset = along.Multiply(half / sine);
        return new LinkHalo(at - offset, at + offset, width);
    }
}
