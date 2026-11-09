using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core;

public delegate BaseLinkModel? LinkFactory(Diagram diagram, ILinkable source, Anchor targetAnchor);

public delegate Anchor AnchorFactory(Diagram diagram, BaseLinkModel link, ILinkable model);

public delegate GroupModel GroupFactory(Diagram diagram, NodeModel[] children);
