using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Models;

/// <summary>
/// A short stroke drawn just under a link where it crosses a link below it, in the background colour, so the lower
/// link reads as broken there. It lies along the upper link, only where the two meet, so it never erases a tee or a
/// port the way a halo along the whole link would.
/// </summary>
/// <param name="Start">Where the stroke starts, in diagram units.</param>
/// <param name="End">Where the stroke ends, in diagram units.</param>
/// <param name="Width">Stroke width in diagram units.</param>
public sealed record LinkHalo(Point Start, Point End, double Width);
