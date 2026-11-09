using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Positions;
using System;
using System.Threading.Tasks;

namespace Flow.Diagrams.Core.Controls.Default;

/// <summary>A draggable arrowhead at one end of a link. Pressing it detaches that end and drags it, so the link can be reconnected elsewhere. Only works on <see cref="BaseLinkModel"/>s and needs <see cref="DragNewLinkBehavior"/>.</summary>
public class ArrowHeadControl : ExecutableControl
{
    /// <summary>Creates the control for the source end when <paramref name="source"/> is true, otherwise the target end; <paramref name="marker"/> sets its size and defaults to a 20 by 20 arrow.</summary>
    public ArrowHeadControl(bool source, LinkMarker? marker = null)
    {
        Source = source;
        Marker = marker ?? LinkMarker.NewArrow(20, 20);
    }

    /// <summary>True for the link's source end, false for its target end.</summary>
    public bool Source { get; }
    /// <summary>The arrowhead drawn for the control; its width also positions the control along the path.</summary>
    public LinkMarker Marker { get; }
    /// <summary>A point just inside the link's end, set back by the difference between this control's marker and the link's own. Throws <see cref="DiagramsException"/> for a model that is not a link.</summary>
    public override Point? GetPosition(Model model)
    {
        var link = AsLink(model);
        var dist = Source ? Marker.Width - (link.SourceMarker?.Width ?? 0) : (link.TargetMarker?.Width ?? 0) - Marker.Width;
        return new LinkPathPositionProvider(dist).GetPosition(link);
    }

    /// <summary>Direction the arrowhead points on <paramref name="model"/>'s end, in degrees; 0 when the link has no path yet. Throws <see cref="DiagramsException"/> for a model that is not a link.</summary>
    public double GetAngle(Model model)
    {
        var link = AsLink(model);
        var p1 = GetPosition(link);
        var p2 = Source ? link.Source.GetPosition(link) : link.Target.GetPosition(link);
        if (p1 is null || p2 is null)
            return 0;

        return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI;
    }

    /// <summary>Starts dragging this control's end of the link; the other end stays attached. Throws <see cref="DiagramsException"/> for a model that is not a link, or when the diagram has no <see cref="DragNewLinkBehavior"/>.</summary>
    public override ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e)
    {
        var link = AsLink(model);
        var dnlb = diagram.GetBehavior<DragNewLinkBehavior>()
                   ?? throw new DiagramsException("DragNewLinkBehavior was not found");

        dnlb.StartFrom(link, e.ClientX, e.ClientY, Source);
        return ValueTask.CompletedTask;
    }

    private static BaseLinkModel AsLink(Model model) => model as BaseLinkModel
        ?? throw new DiagramsException("ArrowHeadControl only works for models of type BaseLinkModel");
}
