using System;

namespace Flow.Diagrams.Core.Models;

/// <summary>
/// A shape drawn at an end of a link, such as an arrowhead. It is an SVG path in a frame where the link end is the
/// origin and the link arrives along the positive x axis; the renderer rotates it to match the link.
/// </summary>
public class LinkMarker
{
    /// <summary>A triangular arrowhead, 10 units long.</summary>
    public static LinkMarker Arrow { get; } = new LinkMarker("M 0 -5 10 0 0 5 z", 10);
    /// <summary>A circle of diameter 10.</summary>
    public static LinkMarker Circle { get; } = new LinkMarker("M 0, 0 a 5,5 0 1,0 10,0 a 5,5 0 1,0 -10,0", 10);
    /// <summary>A square of side 10.</summary>
    public static LinkMarker Square { get; } = new LinkMarker("M 0 -5 10 -5 10 5 0 5 z", 10);

    /// <summary>
    /// Creates a marker from SVG path data; <paramref name="width"/> is its extent along the link, by which the link
    /// line is shortened so it does not show through.
    /// </summary>
    public LinkMarker(string path, double width)
    {
        Path = path;
        Width = width;
    }

    /// <summary>The SVG path data, drawn filled in the link's color.</summary>
    public string Path { get; }
    /// <summary>
    /// The marker's extent along the link in diagram units; the link's path stops this far before the end.
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// A triangular arrowhead <paramref name="width"/> long along the link and <paramref name="height"/> wide across
    /// it.
    /// </summary>
    public static LinkMarker NewArrow(double width, double height)
        => new LinkMarker(FormattableString.Invariant($"M 0 -{height / 2} {width} 0 0 {height / 2}"), width);

    /// <summary>A circle of radius <paramref name="r"/>.</summary>
    public static LinkMarker NewCircle(double r)
        => new LinkMarker(FormattableString.Invariant($"M 0, 0 a {r},{r} 0 1,0 {r * 2},0 a {r},{r} 0 1,0 -{r * 2},0"), r * 2);

    /// <summary>
    /// A rectangle <paramref name="width"/> along the link and <paramref name="height"/> across it.
    /// </summary>
    public static LinkMarker NewRectangle(double width, double height)
        => new LinkMarker(FormattableString.Invariant($"M 0 -{height / 2} {width} -{height / 2} {width} {height / 2} 0 {height / 2} z"), width);

    /// <summary>A square of the given side.</summary>
    public static LinkMarker NewSquare(double size) => NewRectangle(size, size);
}
