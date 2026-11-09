using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Flow.Diagrams.Core.Geometry;

/// <summary>
/// An axis-aligned rectangle in diagram units, stored as its four edges. Y grows downward, so <see cref="Top"/> is
/// normally less than <see cref="Bottom"/>.
/// </summary>
/// <remarks>Equality is by edges: <c>==</c>, <see cref="Equals(Rectangle?)"/> and <see cref="GetHashCode"/> agree, so rectangles work as dictionary keys.</remarks>
public class Rectangle : IShape, IEquatable<Rectangle>
{
    /// <summary>The empty rectangle at the origin.</summary>
    public static Rectangle Zero { get; } = new(0, 0, 0, 0);

    /// <summary>Horizontal extent.</summary>
    public double Width { get; }
    /// <summary>Vertical extent.</summary>
    public double Height { get; }
    /// <summary>Y of the top edge.</summary>
    public double Top { get; }
    /// <summary>X of the right edge.</summary>
    public double Right { get; }
    /// <summary>Y of the bottom edge.</summary>
    public double Bottom { get; }
    /// <summary>X of the left edge.</summary>
    public double Left { get; }

    /// <summary>Creates a rectangle from its edges; swapped edges (<paramref name="left"/> greater than <paramref name="right"/>, or <paramref name="top"/> greater than <paramref name="bottom"/>) are put in order.</summary>
    [JsonConstructor]
    public Rectangle(double left, double top, double right, double bottom)
    {
        Left = Math.Min(left, right);
        Top = Math.Min(top, bottom);
        Right = Math.Max(left, right);
        Bottom = Math.Max(top, bottom);
        Width = Right - Left;
        Height = Bottom - Top;
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
    public bool Intersects(Rectangle r)
        => Left < r.Right && Right > r.Left && Top < r.Bottom && Bottom > r.Top;

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

    /// <summary>The points where the segment <paramref name="line"/> crosses the border; a crossing at a corner is yielded once.</summary>
    public IEnumerable<Point> GetIntersectionsWithLine(Line line)
    {
        var borders = new[] {
            new Line(NorthWest, NorthEast),
            new Line(NorthEast, SouthEast),
            new Line(SouthWest, SouthEast),
            new Line(NorthWest, SouthWest)
        };

        var yielded = new List<Point>(4);
        for (var i = 0; i < borders.Length; i++)
        {
            var intersectionPt = borders[i].GetIntersection(line);
            if (intersectionPt != null && !yielded.Contains(intersectionPt))
            {
                yielded.Add(intersectionPt);
                yield return intersectionPt;
            }
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
        => other is not null && Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Rectangle);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    /// <summary>Whether both rectangles have the same edges, or both are null.</summary>
    public static bool operator ==(Rectangle? left, Rectangle? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Whether the rectangles differ in any edge.</summary>
    public static bool operator !=(Rectangle? left, Rectangle? right) => !(left == right);

    /// <inheritdoc />
    public override string ToString()
                => $"Rectangle(width={Width}, height={Height}, top={Top}, right={Right}, bottom={Bottom}, left={Left})";
}
