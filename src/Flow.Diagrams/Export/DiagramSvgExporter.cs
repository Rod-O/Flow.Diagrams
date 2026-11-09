using System.Text.RegularExpressions;
using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SvgPathProperties;

namespace Flow.Diagrams.Export;

/// <summary>
/// Exports a diagram as a standalone SVG document, rendered statically from the same renderers the canvas uses, so the
/// document matches the screen. Only the SVG layer is exported: HTML nodes and groups are reported and left out.
/// </summary>
public static class DiagramSvgExporter
{
    /// <summary>
    /// Renders the diagram's SVG layer to a standalone SVG document framing everything the visible SVG nodes, SVG groups and
    /// links draw (node and group boxes, their ports, link strokes and markers, and labels) plus
    /// <see cref="SvgExportOptions.Margin"/>; the document is empty but for the margin when there are none. A label's extent
    /// is estimated from its text length and the built-in label's 12-unit font, so a custom label widget that draws larger
    /// may need a wider margin.
    /// </summary>
    /// <param name="diagram">The diagram to export.</param>
    /// <param name="services">
    /// The app's services: the renderers resolve theirs (such as <c>IJSRuntime</c>) from it. The export makes no
    /// JavaScript calls: node renderers skip their resize observer while drawing the document.
    /// </param>
    /// <param name="options">Appearance of the document; <c>null</c> uses the defaults of <see cref="SvgExportOptions"/>.</param>
    /// <remarks>
    /// Models hidden through <see cref="Core.Models.Base.Model.Visible"/> are left out; models hidden only by virtualization
    /// are included: they are shown for the duration of the export (the live diagram redraws them) and hidden again after it.
    /// </remarks>
    public static async Task<SvgExport> ExportAsync(BlazorDiagram diagram, IServiceProvider services, SvgExportOptions? options = null)
    {
        options ??= new SvgExportOptions();
        var virtualization = diagram.GetBehavior<VirtualizationBehavior>();
        var virtualized = virtualization?.HiddenModels.ToList() ?? new List<Model>();
        foreach (var model in virtualized)
            model.Visible = true;
        try
        {
            return await ExportVisibleAsync(diagram, services, options);
        }
        finally
        {
            virtualization?.CheckVisibility();
        }
    }

    private static async Task<SvgExport> ExportVisibleAsync(BlazorDiagram diagram, IServiceProvider services, SvgExportOptions options)
    {
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

    // The built-in SVG label's text size and halo; custom label widgets may differ, so a label's extent is an estimate.
    private const double LabelFontSize = 12;
    private const double LabelHalo = 2;

    /// <summary>
    /// Union of what the visible SVG nodes, SVG groups and links draw: their boxes, their ports' boxes, link paths widened by
    /// half the stroke, marker extents and an estimate of each label's text; null when there is nothing to draw.
    /// </summary>
    private static Rectangle? Bounds(BlazorDiagram diagram)
    {
        var boxes = diagram.OrderedSelectables
            .Where(m => m.Visible)
            .SelectMany(m => m switch
            {
                SvgNodeModel node => NodeBoxes(node),
                SvgGroupModel group => NodeBoxes(group),
                BaseLinkModel link => LinkBoxes(link),
                _ => [],
            })
            .ToList();
        return boxes.Count == 0 ? null : boxes.Aggregate((a, b) => a.Union(b));
    }

    private static IEnumerable<Rectangle> NodeBoxes(NodeModel node)
    {
        if (node.GetBounds() is { } box)
            yield return box;
        foreach (var port in node.Ports.Where(p => p.Visible))
            yield return port.GetBounds();
    }

    private static IEnumerable<Rectangle> LinkBoxes(BaseLinkModel link)
    {
        if (link.PathGeneratorResult is not { } result || link.GetBounds() is not { } path)
            yield break;

        var halfStroke = (link as LinkModel)?.Width / 2 ?? 0;
        yield return path.Inflate(halfStroke, halfStroke);

        if (MarkerBox(link.SourceMarker, result.SourceMarkerPosition, result.SourceMarkerAngle) is { } source)
            yield return source;
        if (MarkerBox(link.TargetMarker, result.TargetMarkerPosition, result.TargetMarkerAngle) is { } target)
            yield return target;

        foreach (var label in link.Labels.Where(l => l.Visible))
        {
            var (x, y) = LinkLabelRenderer.Place(label, result.FullPath);
            var halfWidth = label.Content.Length * 0.6 * LabelFontSize / 2 + LabelHalo;
            var halfHeight = 1.2 * LabelFontSize / 2 + LabelHalo;
            yield return new Rectangle(x - halfWidth, y - halfHeight, x + halfWidth, y + halfHeight);
        }
    }

    /// <summary>The marker's path box, rotated and moved as the link widget draws it.</summary>
    private static Rectangle? MarkerBox(LinkMarker? marker, Point? position, double? angle)
    {
        if (marker == null || position == null || angle == null)
            return null;

        var bbox = new SvgPath(marker.Path).GetBBox();
        var radians = angle.Value * Math.PI / 180;
        var (cos, sin) = (Math.Cos(radians), Math.Sin(radians));
        var corners = new[] { (bbox.Left, bbox.Top), (bbox.Right, bbox.Top), (bbox.Right, bbox.Bottom), (bbox.Left, bbox.Bottom) }
            .Select(c => new Point(position.X + c.Item1 * cos - c.Item2 * sin, position.Y + c.Item1 * sin + c.Item2 * cos))
            .ToList();
        return new Rectangle(corners.Min(c => c.X), corners.Min(c => c.Y), corners.Max(c => c.X), corners.Max(c => c.Y));
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
