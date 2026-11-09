using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Controls;

public abstract class Control
{
    public abstract Point? GetPosition(Model model);
}