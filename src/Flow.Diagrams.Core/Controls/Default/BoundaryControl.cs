using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Controls.Default;

/// <summary>A frame around a model, drawn 10 diagram units outside its bounds. Models without bounds get none.</summary>
public class BoundaryControl : Control
{
    /// <summary>The frame, in diagram units; refreshed by each <see cref="GetPosition"/> call and <see cref="Rectangle.Zero"/> until then.</summary>
    public Rectangle Bounds { get; private set; } = Rectangle.Zero;
    
    /// <summary>The frame's top-left corner, also updating <see cref="Bounds"/>; null when <paramref name="model"/> has no bounds.</summary>
    public override Point? GetPosition(Model model)
    {
        if (model is not IHasBounds hb)
            return null;

        var bounds = hb.GetBounds();
        if (bounds == null)
            return null;

        Bounds = bounds.Inflate(10, 10);
        return Bounds.NorthWest;
    }
}