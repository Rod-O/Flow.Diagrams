using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using System;

namespace Flow.Diagrams.Core.PathGenerators;

/// <summary>
/// Turns a link's route into the SVG path the link is drawn with, and places its end markers. Set per link with
/// <c>BaseLinkModel.PathGenerator</c> or for all links through the diagram's default.
/// </summary>
public abstract class PathGenerator
{
    /// <summary>Builds the path through <paramref name="source"/>, the <paramref name="route"/> points and <paramref name="target"/>.</summary>
    /// <param name="diagram">The diagram that owns the link.</param>
    /// <param name="link">The link being drawn.</param>
    /// <param name="route">The router's intermediate points, excluding the two ends; empty for a direct link.</param>
    /// <param name="source">Where the link leaves its source anchor.</param>
    /// <param name="target">Where the link reaches its target anchor.</param>
    public abstract PathGeneratorResult GetResult(Diagram diagram, BaseLinkModel link, Point[] route, Point source, Point target);

    /// <summary>
    /// Moves <c>route[0]</c> toward <c>route[1]</c> by <paramref name="markerWidth"/> so the line stops where the marker
    /// ends, editing <paramref name="route"/> in place. Returns the marker's angle in degrees; needs at least two points.
    /// </summary>
    protected static double AdjustRouteForSourceMarker(Point[] route, double markerWidth)
    {            
        var angleInRadians = Math.Atan2(route[1].Y - route[0].Y, route[1].X - route[0].X) + Math.PI;
        var xChange = markerWidth * Math.Cos(angleInRadians);
        var yChange = markerWidth * Math.Sin(angleInRadians);
        route[0] = new Point(route[0].X - xChange, route[0].Y - yChange);
        return angleInRadians * 180 / Math.PI;
    }

    /// <summary>
    /// Moves the last point toward the one before it by <paramref name="markerWidth"/> so the line stops where the marker
    /// ends, editing <paramref name="route"/> in place. Returns the marker's angle in degrees; needs at least two points.
    /// </summary>
    protected static double AdjustRouteForTargetMarker(Point[] route, double markerWidth)
    {
        var angleInRadians = Math.Atan2(route[^1].Y - route[^2].Y, route[^1].X - route[^2].X);
        var xChange = markerWidth * Math.Cos(angleInRadians);
        var yChange = markerWidth * Math.Sin(angleInRadians);
        route[^1] = new Point(route[^1].X - xChange, route[^1].Y - yChange);
        return angleInRadians * 180 / Math.PI;
    }

    /// <summary>A new array with <paramref name="source"/>, the <paramref name="route"/> points and <paramref name="target"/>, in that order.</summary>
    protected static Point[] ConcatRouteAndSourceAndTarget(Point[] route, Point source, Point target)
    {
        var result = new Point[route.Length + 2];
        result[0] = source;
        route.CopyTo(result, 1);
        result[^1] = target;
        return result;
    }
}
