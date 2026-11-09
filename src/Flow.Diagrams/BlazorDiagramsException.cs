using System;

namespace Flow.Diagrams;

/// <summary>A rendering problem in the Blazor layer, such as a control with no registered component.</summary>
public class BlazorDiagramsException : Exception
{
    /// <summary>Creates the exception with the given message.</summary>
    public BlazorDiagramsException(string? message) : base(message)
    {
    }
}