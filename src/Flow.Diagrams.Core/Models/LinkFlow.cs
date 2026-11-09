using System;

namespace Flow.Diagrams.Core.Models;

/// <summary>
/// Marching dashes along a link, in its direction from source to target, to show what flows through it. Set on <see
/// cref="LinkModel.Flow"/>. Drawn with CSS only (a dash pattern and a keyframes animation on its offset), static under
/// <c>prefers-reduced-motion: reduce</c>, and left out of the SVG export, which draws the link solid.
/// </summary>
/// <param name="Speed">How fast the dashes move, in diagram units per second; 0 draws still dashes.</param>
/// <param name="Dash">Length of each dash in diagram units; greater than 0.</param>
/// <param name="Gap">Length of the gap between dashes in diagram units; 0 or more.</param>
/// <param name="Reverse">Whether the dashes move from target to source instead.</param>
public sealed record LinkFlow(double Speed = 40, double Dash = 8, double Gap = 6, bool Reverse = false)
{
    private readonly double _speed = AtLeastZero(Speed, nameof(Speed));
    private readonly double _dash = AboveZero(Dash, nameof(Dash));
    private readonly double _gap = AtLeastZero(Gap, nameof(Gap));

    /// <summary>How fast the dashes move, in diagram units per second; 0 draws still dashes. Negative or NaN throws.</summary>
    public double Speed { get => _speed; init => _speed = AtLeastZero(value, nameof(Speed)); }
    /// <summary>Length of each dash in diagram units. 0, negative or NaN throws.</summary>
    public double Dash { get => _dash; init => _dash = AboveZero(value, nameof(Dash)); }
    /// <summary>Length of the gap between dashes in diagram units. Negative or NaN throws.</summary>
    public double Gap { get => _gap; init => _gap = AtLeastZero(value, nameof(Gap)); }

    private static double AtLeastZero(double value, string name) =>
        value >= 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Must be 0 or more.");

    private static double AboveZero(double value, string name) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Must be greater than 0.");
}
