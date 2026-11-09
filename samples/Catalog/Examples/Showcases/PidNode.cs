using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Showcases;

public enum PidKind { Tank, Pump, Valve, ControlValve, CheckValve, Filter, HeatExchanger, Instrument, Panel, Terminal }

/// <summary>
/// A P&amp;ID symbol. Its ports sit where the symbol's pipes attach, not on a box edge: each port is drawn by the widget
/// at <see cref="PortSpots"/>, and its alignment only tells the router which way the line leaves.
/// </summary>
public class PidNode : SvgNodeModel
{
    private static readonly Dictionary<PidKind, (Size Size, double Axis)> Shapes = new()
    {
        [PidKind.Tank] = (new Size(120, 180), 110),
        [PidKind.Pump] = (new Size(64, 64), 32),
        [PidKind.Valve] = (new Size(44, 28), 14),
        [PidKind.ControlValve] = (new Size(44, 60), 46),
        [PidKind.CheckValve] = (new Size(36, 28), 14),
        [PidKind.Filter] = (new Size(64, 48), 24),
        [PidKind.HeatExchanger] = (new Size(150, 64), 32),
        [PidKind.Instrument] = (new Size(44, 44), 22),
        [PidKind.Panel] = (new Size(44, 44), 22),
        [PidKind.Terminal] = (new Size(76, 30), 15),
    };

    /// <param name="left">X of the symbol's left edge.</param>
    /// <param name="axisY">Y of the line its pipes run on; the symbol is placed so that line is at this height.</param>
    public PidNode(PidKind kind, double left, double axisY, string tag, string? caption = null)
        : base(new Point(left, axisY - Shapes[kind].Axis))
    {
        Kind = kind;
        Tag = tag;
        Caption = caption;
        Size = Shapes[kind].Size;
        ControlledSize = true;
        AddPorts();
    }

    public PidKind Kind { get; }
    public string Tag { get; }
    public string? Caption { get; }
    public Dictionary<PortModel, Point> PortSpots { get; } = new();

    /// <summary>The nth port that leaves the symbol in the given direction.</summary>
    public PortModel Port(PortAlignment alignment, int index = 0) => Ports.Where(p => p.Alignment == alignment).ElementAt(index);

    private void AddPorts()
    {
        var (w, h) = (Size!.Width, Size.Height);
        var axis = Shapes[Kind].Axis;
        switch (Kind)
        {
            case PidKind.Tank:
                Spot(PortAlignment.Right, w, axis);
                Spot(PortAlignment.Left, 0, 40);
                Spot(PortAlignment.Top, w / 2, 0);
                break;
            case PidKind.Pump or PidKind.ControlValve:
                Spot(PortAlignment.Left, 0, axis);
                Spot(PortAlignment.Right, w, axis);
                Spot(PortAlignment.Top, w / 2, Kind == PidKind.Pump ? 0 : 4);
                break;
            case PidKind.Valve or PidKind.CheckValve or PidKind.Filter:
                Spot(PortAlignment.Left, 0, axis);
                Spot(PortAlignment.Right, w, axis);
                break;
            case PidKind.HeatExchanger:
                Spot(PortAlignment.Left, 0, axis);
                Spot(PortAlignment.Right, w, axis);
                Spot(PortAlignment.Top, w / 2, 0);
                Spot(PortAlignment.Bottom, 30, h);
                Spot(PortAlignment.Bottom, w - 30, h);
                break;
            default:
                Spot(PortAlignment.Left, 0, h / 2);
                Spot(PortAlignment.Right, w, h / 2);
                Spot(PortAlignment.Top, w / 2, 0);
                Spot(PortAlignment.Bottom, w / 2, h);
                break;
        }
    }

    private void Spot(PortAlignment alignment, double x, double y) => PortSpots[AddPort(alignment)] = new Point(x, y);
}
