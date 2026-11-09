using System;

namespace Flow.Diagrams;

/// <summary>
/// A problem in the Blazor layer, such as a control with no registered component or a duplicate component
/// registration.
/// </summary>
public class FlowDiagramsException : Exception
{
    /// <summary>Creates the exception with the given message.</summary>
    public FlowDiagramsException(string? message) : base(message)
    {
    }
}