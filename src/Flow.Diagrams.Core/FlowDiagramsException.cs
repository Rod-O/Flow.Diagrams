namespace Flow.Diagrams.Core;

/// <summary>
/// A misuse of the library: a duplicate behavior or component registration, a control with no registered component, or
/// a call that needs the canvas before it is measured.
/// </summary>
public class FlowDiagramsException : Exception
{
    /// <summary>Creates the exception with the given message.</summary>
    public FlowDiagramsException(string? message) : base(message)
    {
    }
}