using System.Xml.Linq;
using Bunit;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Export;
using Flow.Diagrams.Models;
using Flow.Diagrams.Options;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class DiagramSvgExporterTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public async Task ExportAsync_ShouldDrawSvgNodesAndLinksInAStandaloneDocument()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var a = diagram.Nodes.Add(new SvgNodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var b = diagram.Nodes.Add(new SvgNodeModel(new Point(300, 0)) { Size = new Size(100, 60) });
        diagram.Links.Add(new LinkModel(a, b));

        // Act
        var export = await DiagramSvgExporter.ExportAsync(diagram, ctx.Services, new SvgExportOptions { Margin = 5, Unit = "mm" });

        // Assert
        var doc = XDocument.Parse(export.Svg);
        doc.Root!.Name.Should().Be(Svg + "svg");
        doc.Root.Attribute("viewBox")!.Value.Should().Be("-5 -5 410 70");
        doc.Root.Attribute("width")!.Value.Should().Be("410mm");
        doc.Descendants(Svg + "g").Count(g => g.Attribute("class")?.Value.StartsWith("diagram-node") == true).Should().Be(2);
        doc.Descendants(Svg + "g").Count(g => g.Attribute("class")?.Value.StartsWith("diagram-link") == true).Should().Be(1);
        doc.Descendants(Svg + "foreignObject").Should().BeEmpty();
        export.HtmlModels.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportAsync_ShouldIncludeModelsHiddenOnlyByVirtualization_AndHideThemAgainAfterwards()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram(new BlazorDiagramOptions { Virtualization = { Enabled = true } });
        diagram.SetContainer(new Rectangle(0, 0, 500, 500));
        diagram.Nodes.Add(new SvgNodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var offScreen = diagram.Nodes.Add(new SvgNodeModel(new Point(3000, 0)) { Size = new Size(100, 60) });
        var hiddenByApp = diagram.Nodes.Add(new SvgNodeModel(new Point(100, 100)) { Size = new Size(100, 60), Visible = false });
        offScreen.Visible.Should().BeFalse();

        // Act
        var export = await DiagramSvgExporter.ExportAsync(diagram, ctx.Services);

        // Assert
        var doc = XDocument.Parse(export.Svg);
        doc.Descendants(Svg + "g").Count(g => g.Attribute("class")?.Value.StartsWith("diagram-node") == true).Should().Be(2);
        doc.Root!.Attribute("viewBox")!.Value.Should().Be("-10 -10 3120 80");
        offScreen.Visible.Should().BeFalse();
        hiddenByApp.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task ExportAsync_ShouldLeaveOutAndReportHtmlNodes()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        diagram.Nodes.Add(new SvgNodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var html = diagram.Nodes.Add(new NodeModel(new Point(300, 0)) { Size = new Size(100, 60) });

        // Act
        var export = await DiagramSvgExporter.ExportAsync(diagram, ctx.Services);

        // Assert
        export.HtmlModels.Should().ContainSingle().Which.Should().Be(html);
        XDocument.Parse(export.Svg).Root!.Attribute("viewBox")!.Value.Should().Be("-10 -10 120 80");
    }

    [Fact]
    public async Task ExportAsync_ShouldReportHtmlLabels_UnlessLabelsAreSvg()
    {
        // Arrange
        using var ctx = new TestContext();
        var diagram = new BlazorDiagram();
        var a = diagram.Nodes.Add(new SvgNodeModel(new Point(0, 0)) { Size = new Size(100, 60) });
        var b = diagram.Nodes.Add(new SvgNodeModel(new Point(300, 0)) { Size = new Size(100, 60) });
        var link = new LinkModel(a, b);
        link.Labels.Add(new LinkLabelModel(link, "4-20 mA"));
        diagram.Links.Add(link);

        // Act
        var htmlLabels = await DiagramSvgExporter.ExportAsync(diagram, ctx.Services);
        diagram.Options.Links.SvgLabels = true;
        var svgLabels = await DiagramSvgExporter.ExportAsync(diagram, ctx.Services);

        // Assert
        htmlLabels.HtmlModels.Should().ContainSingle().Which.Should().Be(link.Labels[0]);
        svgLabels.HtmlModels.Should().BeEmpty();
        var texts = XDocument.Parse(svgLabels.Svg).Descendants(Svg + "text").ToList();
        texts.Select(t => t.Value).Should().Equal("4-20 mA", "4-20 mA");
        texts[0].Attribute("fill")!.Value.Should().Be("none", "the halo comes first, stroke only, under the text");
        texts.Should().OnlyContain(t => t.Attribute("font-family") != null, "a standalone document has no page font to inherit");
    }
}
