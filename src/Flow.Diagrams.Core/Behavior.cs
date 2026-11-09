using System;

namespace Flow.Diagrams.Core;

/// <summary>
/// A unit of interaction logic (selection, dragging, panning, and so on) that subscribes to a <see cref="Diagram"/>'s
/// events. Register one with <see cref="Diagram.RegisterBehavior"/>; <see cref="Dispose"/> must unsubscribe
/// everything it subscribed in its constructor.
/// </summary>
public abstract class Behavior : IDisposable
{
    /// <summary>Binds the behavior to <paramref name="diagram"/>; it does not register it.</summary>
    public Behavior(Diagram diagram)
    {
        Diagram = diagram;
    }

    /// <summary>The diagram this behavior acts on.</summary>
    protected Diagram Diagram { get; }

    /// <summary>
    /// Unsubscribes the behavior from the diagram. Called by <see cref="Diagram.UnregisterBehavior{T}"/>.
    /// </summary>
    public abstract void Dispose();
}
