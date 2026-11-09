using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>A model with a geometric outline, used to find where links meet it.</summary>
public interface IHasShape
{
    /// <summary>The outline in diagram units at the model's current position.</summary>
    public IShape GetShape();
}