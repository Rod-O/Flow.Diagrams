using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Models;

/// <summary>
/// A short stroke drawn just under a link where it crosses a link below it, in the background colour, so the lower
/// link reads as broken there. It lies along the upper link, only where the two meet, so it never erases a tee or a
/// port the way a halo along the whole link would.
/// </summary>
public sealed record LinkHalo(Point Start, Point End, double Width);
