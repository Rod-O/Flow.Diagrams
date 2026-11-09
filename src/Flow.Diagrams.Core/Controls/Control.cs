using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Controls;

/// <summary>A small handle shown next to a model, such as a delete button or a resize grip. <see cref="Control"/> only places it; subclasses of <see cref="ExecutableControl"/> also react to the pointer. Attach controls with <see cref="ControlsContainer.Add"/>.</summary>
public abstract class Control
{
    /// <summary>Where to draw the control for <paramref name="model"/>, in diagram units; null when it cannot be placed yet.</summary>
    public abstract Point? GetPosition(Model model);
}