using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Showcases;

public class OrgChartNode : SvgNodeModel
{
    public OrgChartNode(string name, string title, string color, double centerX, double top) : base()
    {
        Name = name;
        Role = title;
        Color = color;
        Size = new Size(176, 58);
        ControlledSize = true;
        Position = new Point(centerX - Size.Width / 2, top);
        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Bottom);
    }

    public string Name { get; }
    public string Role { get; }
    public string Color { get; }
}
