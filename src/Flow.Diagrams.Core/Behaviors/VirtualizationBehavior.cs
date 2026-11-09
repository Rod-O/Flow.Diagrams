using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Hides models that lie outside the visible area by setting <see cref="Models.Base.Model.Visible"/>, per the kinds enabled in
/// <see cref="Options.DiagramOptions.Virtualization"/>. Rechecked when the zoom, pan or container size changes, not when models are
/// added or moved. Registered by default; does nothing while virtualization is disabled.
/// </summary>
public class VirtualizationBehavior : Behavior
{
    /// <summary>Starts listening to zoom, pan and container changes of <paramref name="diagram"/>.</summary>
    public VirtualizationBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.ZoomChanged += CheckVisibility;
        Diagram.PanChanged += CheckVisibility;
        Diagram.ContainerChanged += CheckVisibility;
    }

    private void CheckVisibility()
    {
        if (!Diagram.Options.Virtualization.Enabled)
            return;
        
        if (Diagram.Container == null)
            return;

        if (Diagram.Options.Virtualization.OnNodes)
        {
            foreach (var node in Diagram.Nodes)
            {
                CheckVisibility(node);
            }
        }

        if (Diagram.Options.Virtualization.OnGroups)
        {
            foreach (var group in Diagram.Groups)
            {
                CheckVisibility(group);
            }
        }

        if (Diagram.Options.Virtualization.OnLinks)
        {
            foreach (var link in Diagram.Links)
            {
                CheckVisibility(link);
            }
        }
    }

    private void CheckVisibility(Model model)
    {
        if (model is not IHasBounds ihb)
            return;
        
        var bounds = ihb.GetBounds();
        if (bounds == null)
            return;
        
        var left = bounds.Left * Diagram.Zoom + Diagram.Pan.X;
        var top = bounds.Top * Diagram.Zoom + Diagram.Pan.Y;
        var right = left + bounds.Width * Diagram.Zoom;
        var bottom = top + bounds.Height * Diagram.Zoom;
        model.Visible = right > 0 && left < Diagram.Container!.Width && bottom > 0 && top < Diagram.Container.Height;
    }

    /// <summary>Stops listening to diagram changes.</summary>
    public override void Dispose()
    {
        Diagram.ZoomChanged -= CheckVisibility;
        Diagram.PanChanged -= CheckVisibility;
        Diagram.ContainerChanged -= CheckVisibility;
    }
}