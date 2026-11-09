using System;
using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.PathGenerators;
using Flow.Diagrams.Core.Routers;

namespace Flow.Diagrams.Core.Options;

public class DiagramLinkOptions
{
    private double _snappingRadius = 50;

    public Router DefaultRouter { get; set; } = new NormalRouter();
    public PathGenerator DefaultPathGenerator { get; set; } = new SmoothPathGenerator();
    public bool EnableSnapping { get; set; } = false;
    public bool RequireTarget { get; set; } = true;

    /// <summary>The colour of link halos (<see cref="Behaviors.LinkHalosBehavior"/>): the diagram's background.</summary>
    public string HaloColor { get; set; } = "white";

    /// <summary>Draw every link label after every link and node, so no line or node covers a label.</summary>
    public bool LabelsOnTop { get; set; } = false;

    /// <summary>
    /// Draw link labels as SVG (a <c>g</c> at the label's position) instead of HTML inside a <c>foreignObject</c>, so the
    /// diagram's SVG holds no HTML and exports as vectors.
    /// </summary>
    public bool SvgLabels { get; set; } = false;

    public double SnappingRadius
    {
        get => _snappingRadius;
        set
        {
            if (value <= 0)
                throw new ArgumentException($"SnappingRadius must be greater than zero");

            _snappingRadius = value;
        }
    }

    public LinkFactory Factory { get; set; } = (diagram, source, targetAnchor) =>
    {
        Anchor sourceAnchor = source switch
        {
            NodeModel node => new ShapeIntersectionAnchor(node),
            PortModel port => new SinglePortAnchor(port),
            _ => throw new NotImplementedException()
        };
        return new LinkModel(sourceAnchor, targetAnchor);
    };

    public AnchorFactory TargetAnchorFactory { get; set; } = (diagram, link, model) =>
    {
        return model switch
        {
            NodeModel node => new ShapeIntersectionAnchor(node),
            PortModel port => new SinglePortAnchor(port),
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
        };
    };
}