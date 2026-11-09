using System.Collections.Generic;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>A model a link can be attached to: nodes, ports and other links.</summary>
public interface ILinkable
{
    /// <summary>The links currently attached to this model, at either end.</summary>
    public IReadOnlyList<BaseLinkModel> Links { get; }

    /// <summary>
    /// Whether a link that starts on this model may end on <paramref name="other"/>; checked while the user drags a
    /// link.
    /// </summary>
    public bool CanAttachTo(ILinkable other);

    internal void AddLink(BaseLinkModel link);

    internal void RemoveLink(BaseLinkModel link);
}