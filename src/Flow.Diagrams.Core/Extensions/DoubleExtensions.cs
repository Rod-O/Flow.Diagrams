using System;

namespace Flow.Diagrams.Core.Extensions;

/// <summary>Extension methods for <see cref="double"/>.</summary>
public static class DoubleExtensions
{
    /// <summary>
    /// Whether the two values differ by less than <paramref name="tolerance"/> (absolute, not relative).
    /// </summary>
    public static bool AlmostEqualTo(this double double1, double double2, double tolerance = 0.0001)
        => Math.Abs(double1 - double2) < tolerance;
}
