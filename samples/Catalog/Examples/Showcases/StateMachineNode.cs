using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Models;

namespace Catalog.Examples.Showcases;

public enum StateKind { Initial, State, Final }

public class StateMachineNode : SvgNodeModel
{
    public StateMachineNode(StateKind kind, double centerX, double centerY, string name = "", string? action = null) : base()
    {
        Kind = kind;
        Name = name;
        Action = action;
        Size = kind switch
        {
            StateKind.Initial => new Size(22, 22),
            StateKind.Final => new Size(30, 30),
            _ => new Size(150, 56),
        };
        ControlledSize = true;
        Position = new Point(centerX - Size.Width / 2, centerY - Size.Height / 2);

        foreach (var alignment in new[] { PortAlignment.Top, PortAlignment.Right, PortAlignment.Bottom, PortAlignment.Left })
            AddPort(alignment);
        if (kind == StateKind.State)
        {
            AddPort(PortAlignment.TopLeft);
            AddPort(PortAlignment.TopRight);
        }
    }

    public StateKind Kind { get; }
    public string Name { get; }
    public string? Action { get; }
}
