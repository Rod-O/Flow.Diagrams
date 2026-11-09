using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace CustomNodesLinks.Models;

public sealed class DiagramLinkLabel : LinkLabelModel
{
public DiagramLinkLabel(BaseLinkModel parent, string id, string content, double? distance = null, Point? offset = null) : 
  base(parent, id, content, distance, offset)
{
}

public DiagramLinkLabel(BaseLinkModel parent, string content, double? distance = null, Point? offset = null) : 
  base(parent, content, distance, offset)
{
}

public bool ShowLabel { get; set; } = true;
}
