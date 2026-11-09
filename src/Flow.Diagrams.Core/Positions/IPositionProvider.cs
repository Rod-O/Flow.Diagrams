using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Positions;

public interface IPositionProvider
{
    public Point? GetPosition(Model model);
}