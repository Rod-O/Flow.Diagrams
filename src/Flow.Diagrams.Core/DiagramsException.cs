using System;

namespace Flow.Diagrams.Core;

/// <summary>The exception type the library throws for diagram errors.</summary>
public class DiagramsException : Exception
{
    /// <summary>Creates the exception with <paramref name="message"/>.</summary>
    public DiagramsException(string? message) : base(message)
    {
    }

    /// <summary>Creates the exception with no message.</summary>
    public DiagramsException() : base()
    {
    }

    /// <summary>Creates the exception with <paramref name="message"/> and the exception that caused it.</summary>
    public DiagramsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
