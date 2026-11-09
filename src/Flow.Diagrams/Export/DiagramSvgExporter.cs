using System.Text.RegularExpressions;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flow.Diagrams.Export;

/// <summary>
/// Exports a diagram as a standalone SVG document, rendered statically from the same renderers the canvas uses, so the
/// document matches the screen. Only the SVG layer is exported: HTML nodes and groups are reported and left out.
/// </summary>
public static class DiagramSvgExporter
{
    /// <param name="services">
    /// The app's services: the renderers resolve theirs (such as <c>IJSRuntime</c>) from it, though a static render
    /// never calls them.
    /// </param>
    public static async Task<SvgExport> ExportAsync(BlazorDiagram diagram, IServiceProvider services, SvgExportOptions? options = null)
    {
        options ??= new SvgExportOptions();
        var html = HtmlModels(diagram);
        var viewBox = Bounds(diagram) is { } b
            ? new Rectangle(b.Left - options.Margin, b.Top - options.Margin, b.Right + options.Margin, b.Bottom + options.Margin)
            : new Rectangle(0, 0, 2 * options.Margin, 2 * options.Margin);

        var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        await using var renderer = new HtmlRenderer(services, loggerFactory);
        var svg = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<SvgExportCanvas>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(SvgExportCanvas.Diagram)] = diagram,
                [nameof(SvgExportCanvas.ViewBox)] = viewBox,
                [nameof(SvgExportCanvas.Options)] = options,
            }));
            return output.ToHtmlString();
        });

        return new SvgExport(InternalAttributes.Replace(svg, ""), html);
    }

    // Blazor's event-handling flags, written valueless as HTML allows and XML does not; meaningless in a static document.
    private static readonly Regex InternalAttributes = new(@"\s__internal_\w+(=""[^""]*"")?", RegexOptions.Compiled);

    /// <summary>Union of the visible SVG nodes, SVG groups and links; null when there is nothing to draw.</summary>
    private static Rectangle? Bounds(BlazorDiagram diagram)
    {
        var boxes = diagram.OrderedSelectables
            .Where(m => m.Visible)
            .Select(m => m switch
            {
                SvgNodeModel node => node.GetBounds(),
                SvgGroupModel group => group.GetBounds(),
                BaseLinkModel link => link.GetBounds(),
                _ => null,
            })
            .OfType<Rectangle>()
            .ToList();
        if (boxes.Count == 0)
            return null;
        return new Rectangle(boxes.Min(r => r.Left), boxes.Min(r => r.Top), boxes.Max(r => r.Right), boxes.Max(r => r.Bottom));
    }

    private static List<Model> HtmlModels(BlazorDiagram diagram)
    {
        var html = new List<Model>();
        html.AddRange(diagram.Nodes.Where(n => n.Visible && n is not SvgNodeModel));
        html.AddRange(diagram.Groups.Where(g => g.Visible && g is not SvgGroupModel));
        if (!diagram.Options.Links.SvgLabels)
            html.AddRange(diagram.Links.Where(l => l.Visible).SelectMany(l => l.Labels));
        return html;
    }
}
