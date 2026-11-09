using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Catalog.Examples.Links;

public enum PortFlow { In, Out }

/// <summary>An output may link to a free input of another node; an input takes one link.</summary>
public class PortRulesPort(NodeModel parent, PortAlignment alignment, PortFlow flow) : PortModel(parent, alignment)
{
    public PortFlow Flow { get; } = flow;

    public override bool CanAttachTo(ILinkable other) =>
        base.CanAttachTo(other) && other is PortRulesPort { Flow: PortFlow.In } input && input.Links.Count == 0;
}
