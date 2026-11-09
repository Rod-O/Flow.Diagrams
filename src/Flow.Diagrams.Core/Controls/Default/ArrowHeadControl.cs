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
    /// <summary>Direction the arrowhead points, in degrees; recomputed on every <see cref="GetPosition"/> call and 0 until then.</summary>
    public double Angle { get; private set; }

    /// <summary>A point just inside the link's end, set back by the difference between this control's marker and the link's own; also updates <see cref="Angle"/>. Throws <see cref="DiagramsException"/> for a model that is not a link.</summary>
    public override Point? GetPosition(Model model)
    {
        if (model is not BaseLinkModel link)
            throw new DiagramsException("ArrowHeadControl only works for models of type BaseLinkModel");

        var dist = Source ? Marker.Width - (link.SourceMarker?.Width ?? 0) : (link.TargetMarker?.Width ?? 0) - Marker.Width;
        var pp = new LinkPathPositionProvider(dist);
        var p1 = pp.GetPosition(link);
        if (p1 is not null)
        {
            var p2 = Source ? link.Source.GetPosition(link) : link.Target.GetPosition(link);
            if (p2 is not null)
            {
                Angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI;
            }
        }

        return p1;
    }

    /// <summary>Starts dragging the end. For the source control the link is first reversed (its source becomes the old target), so the dragged end is always the target. Throws <see cref="DiagramsException"/> for a model that is not a link.</summary>
    public override ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e)
    {
        if (model is not BaseLinkModel link)
            throw new DiagramsException("ArrowHeadControl only works for models of type BaseLinkModel");

        var dnlb = diagram.GetBehavior<DragNewLinkBehavior>()!;
        if (Source)
        {
            link.SetSource(link.Target);
        }

        dnlb.StartFrom(link, e.ClientX, e.ClientY);
        return ValueTask.CompletedTask;
    }
}
