using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Positions;

namespace Flow.Diagrams.Core.Anchors;

/// <summary>Attaches to a point along another link's path, for example to join a link to the middle of another one.</summary>
public class LinkAnchor : Anchor
{
    private readonly LinkPathPositionProvider _positionProvider;

    /// <summary>Creates an anchor on <paramref name="link"/>.</summary>
    /// <param name="link">The link to attach to.</param>
    /// <param name="distance">Where along the path, as for <see cref="LinkPathPositionProvider.Distance"/>: a fraction from 0 to 1, a length greater than 1, or a negative length from the end.</param>
    /// <param name="offsetX">Horizontal shift from that point, in diagram units.</param>
    /// <param name="offsetY">Vertical shift from that point, in diagram units.</param>
    public LinkAnchor(BaseLinkModel link, double distance, double offsetX = 0, double offsetY = 0) : base(link)
    {
        _positionProvider = new LinkPathPositionProvider(distance, offsetX, offsetY);
        Link = link;
    }

    /// <summary>The link this anchor sits on.</summary>
    public BaseLinkModel Link { get; }

    /// <summary>The point on <see cref="Link"/>; ignores <paramref name="link"/> and <paramref name="route"/>. Null until <see cref="Link"/> has been drawn once.</summary>
    public override Point? GetPosition(BaseLinkModel link, Point[] route) => _positionProvider.GetPosition(Link);

    /// <inheritdoc />
    public override Point? GetPlainPosition() => _positionProvider.GetPosition(Link);
}