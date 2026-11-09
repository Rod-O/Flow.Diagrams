using System;

namespace Flow.Diagrams;

public class BlazorDiagramsException : Exception
{
    public BlazorDiagramsException(string? message) : base(message)
    {
    }
}