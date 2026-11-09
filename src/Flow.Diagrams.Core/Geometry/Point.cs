using System;

namespace Flow.Diagrams.Core.Geometry;

/// <summary>An immutable position or vector in diagram units. Every operation returns a new point.</summary>
public record Point
{
    /// <summary>The origin, (0, 0).</summary>
    public static Point Zero { get; } = new(0, 0);

    /// <summary>Creates a point at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Horizontal coordinate; grows to the right.</summary>
    public double X { get; init; }
    /// <summary>Vertical coordinate; grows downward.</summary>
    public double Y { get; init; }

    /// <summary>Distance from the origin, treating the point as a vector.</summary>
    public double Length => Math.Sqrt(Dot(this));

    /// <summary>Dot product with <paramref name="other"/>, treating both points as vectors.</summary>
    public double Dot(Point other) => X * other.X + Y * other.Y;
    /// <summary>The point <paramref name="t"/> of the way from this point to <paramref name="other"/>: 0 is this point, 1 is <paramref name="other"/>, values outside 0..1 extrapolate.</summary>
    public Point Lerp(Point other, double t)
        => new(X * (1.0 - t) + other.X * t, Y * (1.0 - t) + other.Y * t);

    /// <summary>This point with <paramref name="value"/> added to both coordinates.</summary>
    public Point Add(double value) => new(X + value, Y + value);
    /// <summary>This point moved by (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public Point Add(double x, double y) => new(X + x, Y + y);

    /// <summary>This point with <paramref name="value"/> subtracted from both coordinates.</summary>
    public Point Subtract(double value) => new(X - value, Y - value);
    /// <summary>This point moved by (-<paramref name="x"/>, -<paramref name="y"/>).</summary>
    public Point Subtract(double x, double y) => new(X - x, Y - y);
    /// <summary>The vector from <paramref name="other"/> to this point.</summary>
    public Point Subtract(Point other) => new(X - other.X, Y - other.Y);

    /// <summary>Coordinate-wise division by <paramref name="other"/>; a zero coordinate yields infinity or NaN.</summary>
    public Point Divide(Point other) => new(X / other.X, Y / other.Y);

    /// <summary>Both coordinates scaled by <paramref name="value"/>.</summary>
    public Point Multiply(double value) => new(X * value, Y * value);

    /// <summary>The vector scaled to length 1; the zero point stays zero.</summary>
    public Point Normalize()
    {
        var length = Length;
        if (length == 0)
            return Zero;

        return new Point(X / length, Y / length);
    }

    /// <summary>Straight-line distance to <paramref name="other"/>.</summary>
    public double DistanceTo(Point other) => Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    /// <summary>Straight-line distance to (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public double DistanceTo(double x, double y) => Math.Sqrt(Math.Pow(X - x, 2) + Math.Pow(Y - y, 2));

    /// <summary>This point moved <paramref name="dist"/> further along the direction from <paramref name="from"/> to it; a negative distance moves it toward <paramref name="from"/>.</summary>
    public Point MoveAlongLine(Point from, double dist)
    {
        var x = X - from.X;
        var y = Y - from.Y;
        var angle = Math.Atan2(y, x);
        var xOffset = Math.Cos(angle) * dist;
        var yOffset = Math.Sin(angle) * dist;
        return new Point(X + xOffset, Y + yOffset);
    }

    /// <summary>Coordinate-wise difference.</summary>
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    /// <summary>Coordinate-wise sum.</summary>
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>Deconstructs into <c>(x, y)</c>.</summary>
    public void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }
}
