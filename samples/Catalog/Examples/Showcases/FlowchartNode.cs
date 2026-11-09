using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Showcases;

public enum FlowchartShape { Terminal, Process, Decision, Data }

public class FlowchartNode : SvgNodeModel
{
    /// <summary>How far the slanted sides of a data shape lean.</summary>
    public const double Slant = 16;

    public FlowchartNode(FlowchartShape shape, double centerX, double centerY, string text) : base()
    {
        Shape = shape;
        Text = text;
        Size = shape switch
        {
            FlowchartShape.Terminal => new Size(130, 44),
            FlowchartShape.Decision => new Size(140, 84),
            _ => new Size(140, 52),
        };
        ControlledSize = true;
        Position = new Point(centerX - Size.Width / 2, centerY - Size.Height / 2);

        foreach (var alignment in new[] { PortAlignment.Top, PortAlignment.Right, PortAlignment.Bottom, PortAlignment.Left })
            AddPort(alignment);
    }

    public FlowchartShape Shape { get; }
    public string Text { get; }
}
