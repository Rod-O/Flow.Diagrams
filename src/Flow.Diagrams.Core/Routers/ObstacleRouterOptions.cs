namespace Flow.Diagrams.Core.Routers;

/// <summary>
/// Costs that steer <see cref="ObstacleRouter"/>, in diagram units (pixels by default). The router picks the route with
/// the lowest length + bends + crossings + overlap; raising a cost makes the router pay more length to avoid it.
/// </summary>
public sealed record ObstacleRouterOptions
{
    /// <summary>Clearance kept around every node.</summary>
    public double Margin { get; init; } = 10;

    /// <summary>What one bend costs, in units of length.</summary>
    public double BendCost { get; init; } = 30;

    /// <summary>What crossing a link of another net costs, in units of length.</summary>
    public double CrossCost { get; init; } = 200;

    /// <summary>Cost per unit of length of running on top of a link of another net.</summary>
    public double OverlapCostPerUnit { get; init; } = 40;

    /// <summary>Cost per unit of length of running closer than the margin alongside a link of another net.</summary>
    public double HugCostPerUnit { get; init; } = 20;

    /// <summary>Straight run kept before an end that carries a marker, so the arrowhead has a leg to sit on.</summary>
    public double MinStub { get; init; } = 20;

    /// <summary>Distance of the outer ring of routing lines from everything, so a route can always go around.</summary>
    public double OuterRing { get; init; } = 30;
}
