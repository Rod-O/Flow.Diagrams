using System;

namespace Flow.Diagrams;

/// <summary>
/// A problem in the Blazor layer, such as a control with no registered component or a duplicate component
/// registration.
/// </summary>
public class BlazorDiagramsException : Exception
{
    /// <summary>Creates the exception with the given message.</summary>
    public BlazorDiagramsException(string? message) : base(message)
    {
    }
}