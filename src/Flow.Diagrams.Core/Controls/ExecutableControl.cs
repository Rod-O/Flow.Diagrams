using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Models.Base;
using System.Threading.Tasks;

namespace Flow.Diagrams.Core.Controls;

public abstract class ExecutableControl : Control
{
    public abstract ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e);
}