using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Site.Models.Ports;

public class MyCustomPortModel : PortModel
{
    public bool In { get; set; }

    public MyCustomPortModel(NodeModel parent, bool @in, PortAlignment alignment = PortAlignment.Bottom, Point? position = null, Size? size = null) : base(parent, alignment, position, size)
    {
        In = @in;
    }

    public MyCustomPortModel(string id, NodeModel parent, bool @in, PortAlignment alignment = PortAlignment.Bottom, Point? position = null, Size? size = null) : base(id, parent, alignment, position, size)
    {
        In = @in;
    }

    public override bool CanAttachTo(ILinkable other)
    {
        if (!base.CanAttachTo(other)) // default constraints
            return false;

        if (other is not MyCustomPortModel otherPort)
            return false;

        // Only link Ins with Outs
        return In != otherPort.In;
    }
}
