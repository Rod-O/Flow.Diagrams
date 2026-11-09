using Flow.Diagrams.Core.Options;

namespace Flow.Diagrams.Core.Tests;

public class TestDiagram : Diagram
{
    public TestDiagram(DiagramOptions? options = null)
    {
        Options = options ?? new DiagramOptions();
    }

    public override DiagramOptions Options { get; }
}