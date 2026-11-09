using System.Globalization;

namespace Flow.Diagrams.Core.Extensions;

/// <summary>Extension methods for numbers.</summary>
public static class NumberExtensions
{
    /// <summary>Formats the number with the invariant culture, as SVG attributes and paths require.</summary>
    public static string ToInvariantString(this double n) => n.ToString(CultureInfo.InvariantCulture);
}
