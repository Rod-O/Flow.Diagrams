using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Showcases;

public enum NetworkKind { Internet, Firewall, CoreSwitch, AccessSwitch, Server, Workstation, AccessPoint }

public class NetworkTopologyNode : SvgNodeModel
{
    public NetworkTopologyNode(NetworkKind kind, double centerX, double top, string name, string? address = null)
        : base(new Point(0, top))
    {
        Kind = kind;
        Name = name;
        Address = address;
        Size = kind switch
        {
            NetworkKind.Internet => new Size(130, 76),
            NetworkKind.Firewall => new Size(96, 60),
            NetworkKind.CoreSwitch => new Size(220, 48),
            NetworkKind.AccessSwitch => new Size(140, 36),
            _ => new Size(64, 64),
        };
        ControlledSize = true;
        Position = new Point(centerX - Size.Width / 2, top);

        if (kind != NetworkKind.Internet)
            AddPort(PortAlignment.Top);
        if (kind is NetworkKind.Internet or NetworkKind.Firewall or NetworkKind.CoreSwitch or NetworkKind.AccessSwitch)
            AddPort(PortAlignment.Bottom);
    }

    public NetworkKind Kind { get; }
    public string Name { get; }
    public string? Address { get; }
}
