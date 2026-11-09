using BenchmarkDotNet.Attributes;

namespace Flow.Diagrams.Benchmarks;

/// <summary>Creating a diagram with the default behaviors and adding the grid's nodes and links, routed by the defaults.</summary>
[MemoryDiagnoser]
public class BuildBenchmarks
{
    [Params(50, 200, 1000)] public int Nodes { get; set; }

    [Benchmark]
    public BlazorDiagram Build()
    {
        var diagram = new BlazorDiagram();
        GridLayout.AddLinks(diagram, GridLayout.AddNodes(diagram, Nodes));
        return diagram;
    }
}
