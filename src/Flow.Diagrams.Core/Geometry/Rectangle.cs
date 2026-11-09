using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Flow.Diagrams.Core.Geometry;

/// <summary>
/// An axis-aligned rectangle in diagram units, stored as its four edges. Y grows downward, so <see cref="Top"/> is
/// normally less than <see cref="Bottom"/>.
/// </summary>
/// <remarks>
/// Equality is by edges through <see cref="Equals(Rectangle?)"/> only; <c>object.Equals</c> and <c>GetHashCode</c> are
/// reference-based, so do not use rectangles as dictionary keys expecting value equality.
/// </remarks>
public class Rectangle : IShape
{
    /// <summary>The empty rectangle at the origin.</summary>
    public static Rectangle Zero { get; } = new(0, 0, 0, 0);

    /// <summary>Horizontal extent; never negative, even if the edges were given reversed.</summary>
    public double Width { get; }
    /// <summary>Vertical extent; never negative, even if the edges were given reversed.</summary>
    public double Height { get; }
    /// <summary>Y of the top edge.</summary>
    public double Top { get; }
    /// <summary>X of the right edge.</summary>
    public double Right { get; }
    /// <summary>Y of the bottom edge.</summary>
    public double Bottom { get; }
    /// <summary>X of the left edge.</summary>
    public double Left { get; }

    /// <summary>Creates a rectangle from its edges. They are not reordered: swapped edges give a positive <see cref="Width"/> but <see cref="Left"/> greater than <see cref="Right"/>.</summary>
    [JsonConstructor]
    public Rectangle(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Width = Math.Abs(Left - Right);
        Height = Math.Abs(Top - Bottom);
    }

    /// <summary>Creates a rectangle with its top-left corner at <paramref name="position"/>; throws when either argument is null.</summary>
    public Rectangle(Point position, Size size)
    {
        ArgumentNullException.ThrowIfNull(position, nameof(position));
        ArgumentNullException.ThrowIfNull(size, nameof(size));

        Left = position.X;
        Top = position.Y;
        Right = Left + size.Width;
        Bottom = Top + size.Height;
        Width = size.Width;
        Height = size.Height;
    }

    /// <summary>Whether the interiors of the two rectangles overlap; rectangles that only share an edge do not.</summary>
    public bool Overlap(Rectangle r)
        => Left < r.Right && Right > r.Left && Top < r.Bottom && Bottom > r.Top;

    /// <summary>Whether the interiors of the two rectangles overlap; same result as <see cref="Overlap"/>, which reads the edges directly.</summary>
    public bool Intersects(Rectangle r)
    {
        var thisX = Left;
        var thisY = Top;
        var thisW = Width;
        var thisH = Height;
        var rectX = r.Left;
        var rectY = r.Top;
        var rectW = r.Width;
        var rectH = r.Height;
        return rectX < thisX + thisW && thisX < rectX + rectW && rectY < thisY + thisH && thisY < rectY + rectH;
    }

    /// <summary>A rectangle grown by <paramref name="horizontal"/> on the left and the right and by <paramref name="vertical"/> on the top and the bottom; negative values shrink it.</summary>
    public Rectangle Inflate(double horizontal, double vertical)
        => new(Left - horizontal, Top - vertical, Right + horizontal, Bottom + vertical);

    /// <summary>The smallest rectangle that contains this one and <paramref name="r"/>.</summary>
    public Rectangle Union(Rectangle r)
    {
        var x1 = Math.Min(Left, r.Left);
        var x2 = Math.Max(Left + Width, r.Left + r.Width);
        var y1 = Math.Min(Top, r.Top);
        var y2 = Math.Max(Top + Height, r.Top + r.Height);
        return new(x1, y1, x2, y2);
    }

    /// <summary>Whether <paramref name="point"/> is inside or on the edge.</summary>
    public bool ContainsPoint(Point point) => ContainsPoint(point.X, point.Y);

    /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) is inside or on the edge.</summary>
    public bool ContainsPoint(double x, double y)
        => x >= Left && x <= Right && y >= Top && y <= Bottom;

    /// <summary>The points where the segment <paramref name="line"/> crosses the border; a crossing exactly at a corner can be yielded twice.</summary>
    public IEnumerable<Point> GetIntersectionsWithLine(Line line)
    {
        var borders = new[] {
            new Line(NorthWest, NorthEast),
            new Line(NorthEast, SouthEast),
            new Line(SouthWest, SouthEast),
            new Line(NorthWest, SouthWest)
        };

        for (var i = 0; i < borders.Length; i++)
        {
            var intersectionPt = borders[i].GetIntersection(line);
            if (intersectionPt != null)
                yield return intersectionPt;
        }
    }

    /// <summary>The border point hit by a ray from the center in direction <paramref name="a"/> (degrees, 0 = right, 90 = down); null for a zero-size rectangle.</summary>
    public Point? GetPointAtAngle(double a)
    {
        var vx = Math.Cos(a * Math.PI / 180);
        var vy = Math.Sin(a * Math.PI / 180);
        var px = Left + Width / 2;
        var py = Top + Height / 2;
        double? t1 = (Left - px) / vx; // left
        double? t2 = (Right - px) / vx; // right
        double? t3 = (Top - py) / vy; // top
        double? t4 = (Bottom - py) / vy; // bottom
        var t = (new[] { t1, t2, t3, t4 }).Where(n => n.HasValue && double.IsFinite(n.Value) && n.Value > 0).DefaultIfEmpty(null).Min();
        if (t == null) return null;

        var x = px + t.Value * vx;
        var y = py + t.Value * vy;
        return new Point(x, y);
    }

    /// <summary>The middle of the rectangle.</summary>
    public Point Center => new(Left + Width / 2, Top + Height / 2);
    /// <summary>Top-right corner.</summary>
    public Point NorthEast => new(Right, Top);
    /// <summary>Bottom-right corner.</summary>
    public Point SouthEast => new(Right, Bottom);
    /// <summary>Bottom-left corner.</summary>
    public Point SouthWest => new(Left, Bottom);
    /// <summary>Top-left corner.</summary>
    public Point NorthWest => new(Left, Top);
    /// <summary>Middle of the right edge.</summary>
    public Point East => new(Right, Top + Height / 2);
    /// <summary>Middle of the top edge.</summary>
    public Point North => new(Left + Width / 2, Top);
    /// <summary>Middle of the bottom edge.</summary>
    public Point South => new(Left + Width / 2, Bottom);
    /// <summary>Middle of the left edge.</summary>
    public Point West => new(Left, Top + Height / 2);

    /// <summary>Whether <paramref name="other"/> has the same edges.</summary>
    public bool Equals(Rectangle? other)
    {
        return other != null && Left == other.Left && Right == other.Right && Top == other.Top &&
            Bottom == other.Bottom && Width == other.Width && Height == other.Height;
    }

    /// <inheritdoc />
    public override string ToString()
                => $"Rectangle(width={Width}, height={Height}, top={Top}, right={Right}, bottom={Bottom}, left={Left})";
}
