using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Models.Base;
using System.Linq;

namespace Flow.Diagrams.Core.Layers;

/// <summary>The links of a diagram. Adding a link attaches it to its anchors' models and routes it.</summary>
public class LinkLayer : BaseLayer<BaseLinkModel>
{
    /// <summary>Creates the layer for <paramref name="diagram"/>.</summary>
    public LinkLayer(Diagram diagram) : base(diagram) { }

    /// <summary>
    /// Sets the link's <see cref="BaseLinkModel.Diagram"/>, registers it with the models its anchors point to,
    /// refreshes it and follows later anchor changes.
    /// </summary>
    protected override void OnItemAdded(BaseLinkModel link)
    {
        link.Diagram = Diagram;
        HandleAnchor(link, link.Source, true);
        HandleAnchor(link, link.Target, true);
        link.MarkChanged(ModelChanges.Geometry);

        link.SourceChanged += OnLinkSourceChanged;
        link.TargetChanged += OnLinkTargetChanged;
    }

    /// <summary>
    /// Clears the link's diagram, unregisters it from its anchors' models, removes its controls and removes the links
    /// attached to it.
    /// </summary>
    protected override void OnItemRemoved(BaseLinkModel link)
    {
        link.Diagram = null;
        HandleAnchor(link, link.Source, false);
        HandleAnchor(link, link.Target, false);
        link.MarkChanged(ModelChanges.Geometry);

        link.SourceChanged -= OnLinkSourceChanged;
        link.TargetChanged -= OnLinkTargetChanged;
        
        Diagram.Controls.RemoveFor(link);
        Remove(link.Links.ToList());
    }

    private static void OnLinkSourceChanged(BaseLinkModel link, Anchor old, Anchor @new)
    {
        HandleAnchor(link, old, add: false);
        HandleAnchor(link, @new, add: true);
    }

    private static void OnLinkTargetChanged(BaseLinkModel link, Anchor old, Anchor @new)
    {
        HandleAnchor(link, old, add: false);
        HandleAnchor(link, @new, add: true);
    }

    private static void HandleAnchor(BaseLinkModel link, Anchor anchor, bool add)
    {
        if (add)
        {
            anchor.Model?.AddLink(link);
        }
        else
        {
            anchor.Model?.RemoveLink(link);
        }

        if (anchor.Model is Model model)
        {
            model.MarkChanged(ModelChanges.Appearance);
        }
    }
}
