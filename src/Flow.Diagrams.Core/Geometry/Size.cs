namespace Flow.Diagrams.Core.Geometry;

/// <summary>An immutable width and height in diagram units.</summary>
public record Size
{
    /// <summary>A size of 0 by 0.</summary>
    public static Size Zero { get; } = new(0, 0);

    /// <summary>Creates a size of <paramref name="width"/> by <paramref name="height"/>.</summary>
    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Horizontal extent.</summary>
    public double Width { get; init; }
    /// <summary>Vertical extent.</summary>
    public double Height { get; init; }
}
