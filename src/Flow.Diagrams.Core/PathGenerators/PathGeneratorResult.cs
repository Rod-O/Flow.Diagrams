using Flow.Diagrams.Core.Geometry;
using SvgPathProperties;

namespace Flow.Diagrams.Core.PathGenerators;

/// <summary>What a <see cref="PathGenerators.PathGenerator"/> produced for one link: the path to draw and where its end markers go.</summary>
public class PathGeneratorResult
{
    /// <summary>Creates a result; the marker arguments are null for an end without a marker.</summary>
    public PathGeneratorResult(SvgPath fullPath, SvgPath[] paths, double? sourceMarkerAngle = null, Point? sourceMarkerPosition = null,
        double? targetMarkerAngle = null, Point? targetMarkerPosition = null)
    {
        FullPath = fullPath;
        Paths = paths;
        SourceMarkerAngle = sourceMarkerAngle;
        SourceMarkerPosition = sourceMarkerPosition;
        TargetMarkerAngle = targetMarkerAngle;
        TargetMarkerPosition = targetMarkerPosition;
    }

    /// <summary>The whole link as one path, from source to target.</summary>
    public SvgPath FullPath { get; }
    /// <summary>The same line split into one path per segment, so each piece can be styled or measured on its own; empty when the generator does not split it.</summary>
    public SvgPath[] Paths { get; }
    /// <summary>Rotation of the source marker in degrees; null when the link has no source marker.</summary>
    public double? SourceMarkerAngle { get; }
    /// <summary>Where the source marker sits, which is the start of <see cref="FullPath"/> after it was shortened to make room for the marker.</summary>
    public Point? SourceMarkerPosition { get; }
    /// <summary>Rotation of the target marker in degrees; null when the link has no target marker.</summary>
    public double? TargetMarkerAngle { get; }
    /// <summary>Where the target marker sits, which is the end of <see cref="FullPath"/> after it was shortened to make room for the marker.</summary>
    public Point? TargetMarkerPosition { get; }
}
