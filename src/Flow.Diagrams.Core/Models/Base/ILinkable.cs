using System.Collections.Generic;

namespace Flow.Diagrams.Core.Models.Base;

public interface ILinkable
{
    public IReadOnlyList<BaseLinkModel> Links { get; }

    public bool CanAttachTo(ILinkable other);

    internal void AddLink(BaseLinkModel link);

    internal void RemoveLink(BaseLinkModel link);
}