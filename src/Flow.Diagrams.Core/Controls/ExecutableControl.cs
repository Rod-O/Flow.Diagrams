using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Models.Base;
using System.Threading.Tasks;

namespace Flow.Diagrams.Core.Controls;

/// <summary>A <see cref="Control"/> that does something when pressed, such as deleting its model or starting a link.</summary>
public abstract class ExecutableControl : Control
{
    /// <summary>Runs when the pointer is pressed on the control.</summary>
    /// <param name="diagram">The diagram the control is in.</param>
    /// <param name="model">The model the control belongs to.</param>
    /// <param name="e">The pointer event.</param>
    public abstract ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e);
}