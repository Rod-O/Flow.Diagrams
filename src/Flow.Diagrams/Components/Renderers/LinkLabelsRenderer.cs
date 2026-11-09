using Flow.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Flow.Diagrams.Components.Renderers;

/// <summary>
/// One link's labels, drawn apart from the link when <see cref="Core.Options.DiagramLinkOptions.LabelsOnTop"/> is set,
/// so the canvas can draw every label after everything else. Redraws when the link changes, since its labels sit on
/// its path.
/// </summary>
public class LinkLabelsRenderer : ComponentBase, IDisposable
{
    /// <summary>The link whose labels are drawn; required.</summary>
    [Parameter] public BaseLinkModel Link { get; set; } = null!;

    /// <inheritdoc />
    public void Dispose()
    {
        Link.Changed -= OnLinkChanged;
        Link.VisibilityChanged -= OnLinkChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Link.Changed += OnLinkChanged;
        Link.VisibilityChanged += OnLinkChanged;
    }

    /// <summary>Draws the link's labels along its path; nothing when the link is invisible, has no labels or has no path yet.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Link.Visible || Link.Labels.Count == 0 || Link.PathGeneratorResult is not { } result)
            return;

        builder.OpenElement(0, "g");
        builder.AddAttribute(1, "class", "diagram-link-labels");
        builder.AddAttribute(2, "data-link-id", Link.Id);
        foreach (var label in Link.Labels)
        {
            builder.OpenComponent<LinkLabelRenderer>(3);
            builder.SetKey(label.Id);
            builder.AddAttribute(4, nameof(LinkLabelRenderer.Label), label);
            builder.AddAttribute(5, nameof(LinkLabelRenderer.Path), result.FullPath);
            builder.CloseComponent();
        }
        builder.CloseElement();
    }

    private void OnLinkChanged(Core.Models.Base.Model _) => InvokeAsync(StateHasChanged);
}
