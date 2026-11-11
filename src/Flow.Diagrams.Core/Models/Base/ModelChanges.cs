using System;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>What a <see cref="Model.Changed"/> notification says changed, so a subscriber can skip work it does not need.</summary>
[Flags]
public enum ModelChanges
{
    /// <summary>Nothing changed.</summary>
    None = 0,
    /// <summary>The model must be redrawn with its current geometry.</summary>
    Appearance = 1,
    /// <summary>Derived geometry (route, path, group bounds) must be recomputed before redrawing; always raised together with <see cref="Appearance"/>.</summary>
    Geometry = 2
}
