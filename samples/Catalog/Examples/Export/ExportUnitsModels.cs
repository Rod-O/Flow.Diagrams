using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Export;

/// <summary>A device on a mounting rail. Coordinates and sizes are millimetres.</summary>
public class ExportUnitsDevice : SvgNodeModel
{
    public ExportUnitsDevice(string label, string fill, double x, double y, double width, double height) : base(new Point(x, y))
    {
        Label = label;
        Fill = fill;
        Size = new Size(width, height);
        ControlledSize = true;
        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Bottom);
    }

    public string Label { get; }
    public string Fill { get; }
}

/// <summary>The backplate: a group that frames the devices and draws the rails they are mounted on.</summary>
public class ExportUnitsPlate(IEnumerable<NodeModel> devices, IReadOnlyList<double> railTops)
    : SvgGroupModel(devices, new GroupPadding(Left: 30, Top: 50, Right: 30, Bottom: 30))
{
    public const double RailHeight = 35;

    public IReadOnlyList<double> RailTops { get; } = railTops;
}
