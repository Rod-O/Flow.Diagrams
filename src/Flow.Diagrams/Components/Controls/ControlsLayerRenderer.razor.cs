using Flow.Diagrams.Core;
using System;
using System.Threading.Tasks;
using Flow.Diagrams.Core.Controls;
using Flow.Diagrams.Core.Extensions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Flow.Diagrams.Components.Controls;

/// <summary>
/// Draws the controls (remove button, link handle, boundary, arrowheads) of every model that has them, in one layer: SVG or
/// HTML. <see cref="Components.DiagramCanvas"/> includes one of each. It re-renders when the controls of a model of its layer change.
/// </summary>
public partial class ControlsLayerRenderer : IDisposable
{
    private bool _shouldRender;

    /// <summary>The diagram whose controls are drawn.</summary>
    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;

    /// <summary>Whether this instance draws the controls of SVG models (<c>true</c>) or of HTML models (<c>false</c>); see <see cref="Extensions.ModelExtensions.IsSvg"/>.</summary>
    [Parameter] public bool Svg { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        BlazorDiagram.Controls.ChangeCaused -= OnControlsChangeCaused;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        BlazorDiagram.Controls.ChangeCaused += OnControlsChangeCaused;
    }

    /// <summary>Renders only after the diagram's controls changed for a model of this instance's layer.</summary>
    protected override bool ShouldRender() => _shouldRender;

    private void OnControlsChangeCaused(Model cause)
    {
        if (Svg != cause.IsSvg())
            return;

        _shouldRender = true;
        InvokeAsync(StateHasChanged);
    }

    private RenderFragment RenderControl(Model model, Control control, Point position, bool svg)
    {
        var componentType = BlazorDiagram.GetComponent(control.GetType());
        if (componentType == null)
            throw new DiagramsException(
                $"A component couldn't be found for the user action {control.GetType().Name}");

        return builder =>
        {
            builder.OpenElement(0, svg ? "g" : "div");
            builder.AddAttribute(1, "class",
                $"{(control is ExecutableControl ? "executable " : "")}diagram-control {control.GetType().Name}");
            if (svg)
                builder.AddAttribute(2, "transform",
                    $"translate({position.X.ToInvariantString()} {position.Y.ToInvariantString()})");
            else
                builder.AddAttribute(2, "style",
                    $"top: {position.Y.ToInvariantString()}px; left: {position.X.ToInvariantString()}px");

            if (control is ExecutableControl ec)
            {
                builder.AddAttribute(3, "onpointerdown",
                    EventCallback.Factory.Create<PointerEventArgs>(this, e => OnPointerDown(e, model, ec)));
                builder.AddEventStopPropagationAttribute(4, "onpointerdown", true);
            }

            builder.OpenComponent(5, componentType);
            builder.AddAttribute(6, "Control", control);
            builder.AddAttribute(7, "Model", model);
            builder.CloseComponent();
            builder.CloseElement();
        };
    }

    private async Task OnPointerDown(PointerEventArgs e, Model model, ExecutableControl control)
    {
        if (e.Button == 0 || e.Buttons == 1) await control.OnPointerDown(BlazorDiagram, model, e.ToCore());
    }
}