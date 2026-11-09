using System;
using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.PathGenerators;
using Flow.Diagrams.Core.Routers;

namespace Flow.Diagrams.Core.Options;

/// <summary>Options for links and for creating them by dragging.</summary>
public class DiagramLinkOptions
{
    private double _snappingRadius = 50;

    /// <summary>
    /// The router for links whose <see cref="Models.Base.BaseLinkModel.Router"/> is <c>null</c>; <see cref="Routers.NormalRouter"/> by
    /// default.
    /// </summary>
    public Router DefaultRouter { get; set; } = new NormalRouter();
    /// <summary>
    /// The path generator for links whose <see cref="Models.Base.BaseLinkModel.PathGenerator"/> is <c>null</c>; <see
    /// cref="SmoothPathGenerator"/> by default.
    /// </summary>
    public PathGenerator DefaultPathGenerator { get; set; } = new SmoothPathGenerator();
    /// <summary>
    /// Whether a link being dragged jumps to the nearest attachable port within <see cref="SnappingRadius"/> instead
    /// of waiting for the pointer to be released on it.
    /// </summary>
    public bool EnableSnapping { get; set; } = false;
    /// <summary>
    /// Whether a dragged link that is released on nothing attachable is removed (<c>true</c>) or stays with a free
    /// end (<c>false</c>).
    /// </summary>
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

    /// <summary>
    /// How close, in diagram units, the dragged end must come to a port to snap to it; 50 by default. Setting zero or
    /// less throws <see cref="ArgumentException"/>.
    /// </summary>
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

    /// <summary>
    /// Creates the link that starts when the user drags from a port; returning <c>null</c> starts no link. The
    /// default makes a <see cref="LinkModel"/> anchored to the node's shape or the port, and throws <see
    /// cref="NotImplementedException"/> for any other source.
    /// </summary>
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

    /// <summary>
    /// Creates the anchor for the end of a dragged link dropped on a node or port. The default uses the node's shape
    /// or the port, and throws <see cref="ArgumentOutOfRangeException"/> for any other model.
    /// </summary>
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