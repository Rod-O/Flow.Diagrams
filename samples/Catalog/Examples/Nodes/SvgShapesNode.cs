using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Nodes;

public enum SvgShape { Circle, Diamond, Hexagon, Box }

/// <summary>
/// An SVG node whose size the model sets. ControlledSize stops the library measuring it: the measured box would include
/// the port circles that stick out of the shape, the widget would grow to it, and be measured bigger again.
/// </summary>
public class SvgShapesNode : SvgNodeModel
{
    public SvgShapesNode(Point position, SvgShape shape, string label) : base(position)
    {
        Shape = shape;
        Label = label;
        Size = shape == SvgShape.Box ? new Size(140, 70) : new Size(100, 100);
        ControlledSize = true;
    }

    public SvgShape Shape { get; }
    public string Label { get; }
}
