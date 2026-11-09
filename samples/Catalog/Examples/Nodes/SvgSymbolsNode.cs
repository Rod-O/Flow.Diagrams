using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Nodes;

public enum Symbol { Tank, Pump, Valve, Motor, Sensor }

/// <summary>A process symbol. ControlledSize: the model owns the size, so the measured ports cannot enlarge it.</summary>
public class SvgSymbolsNode : SvgNodeModel
{
    public SvgSymbolsNode(Point position, Symbol symbol, string tag) : base(position)
    {
        Symbol = symbol;
        Tag = tag;
        Size = symbol == Symbol.Tank ? new Size(90, 120) : new Size(70, 70);
        ControlledSize = true;
    }

    public Symbol Symbol { get; }
    public string Tag { get; }
}
