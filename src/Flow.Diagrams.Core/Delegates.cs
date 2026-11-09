using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core;

/// <summary>
/// Creates the link shown while the user drags from <paramref name="source"/>, or <c>null</c> to start none;
/// <paramref name="targetAnchor"/> follows the pointer until the link is attached. Used by <see
/// cref="Options.DiagramLinkOptions.Factory"/>.
/// </summary>
public delegate BaseLinkModel? LinkFactory(Diagram diagram, ILinkable source, Anchor targetAnchor);

/// <summary>
/// Creates the anchor that attaches <paramref name="link"/> to <paramref name="model"/>, the model it was dropped on.
/// Used by <see cref="Options.DiagramLinkOptions.TargetAnchorFactory"/>.
/// </summary>
public delegate Anchor AnchorFactory(Diagram diagram, BaseLinkModel link, ILinkable model);

/// <summary>
/// Creates the group that wraps <paramref name="children"/>. Used by <see
/// cref="Options.DiagramGroupOptions.Factory"/>.
/// </summary>
public delegate GroupModel GroupFactory(Diagram diagram, NodeModel[] children);
