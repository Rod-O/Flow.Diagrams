using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Controls;
using Flow.Diagrams.Core.Events;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Microsoft.JSInterop;

namespace Site.Models.Controls;

public class AlertControl : ExecutableControl
{
    private readonly IJSRuntime _jSRuntime;

    public AlertControl(IJSRuntime jSRuntime)
    {
        _jSRuntime = jSRuntime;
    }

    public override Point? GetPosition(Model model)
    {
        // Fixed at top-right
        var node = (model as NodeModel)!;
        if (node.Size == null)
            return null;

        return node.Position.Add(node.Size.Width, -15);
    }

    public override async ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e)
    {
        await _jSRuntime.InvokeVoidAsync("alert", "Some Alert??");
    }
}
