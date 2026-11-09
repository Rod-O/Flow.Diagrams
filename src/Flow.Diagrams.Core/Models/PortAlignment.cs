namespace Flow.Diagrams.Core.Models;

/// <summary>
/// Where a port sits on its node. Anchors and routers use it to choose the direction a link leaves the port.
/// </summary>
public enum PortAlignment
{
    /// <summary>Centered on the top side.</summary>
    Top,
    /// <summary>At the top-right corner.</summary>
    TopRight,
    /// <summary>Centered on the right side.</summary>
    Right,
    /// <summary>At the bottom-right corner.</summary>
    BottomRight,
    /// <summary>Centered on the bottom side.</summary>
    Bottom,
    /// <summary>At the bottom-left corner.</summary>
    BottomLeft,
    /// <summary>Centered on the left side.</summary>
    Left,
    /// <summary>At the top-left corner.</summary>
    TopLeft
}
