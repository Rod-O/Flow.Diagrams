using BenchmarkDotNet.Attributes;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.PathGenerators;

namespace Flow.Diagrams.Benchmarks;

/// <summary>
/// Moving one node in the middle of the grid with <see cref="LinkHalosBehavior"/> registered and straight links: the
/// move refreshes the node's links, and the behavior recomputes the halos of each and of every link drawn above it.
/// </summary>
[MemoryDiagnoser]
public class LinkHalosBenchmarks
{
    private NodeModel _node = null!;
    private bool _moved;

    [Params(50, 200, 1000)] public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var diagram = new BlazorDiagram();
        diagram.Options.Links.DefaultPathGenerator = new StraightPathGenerator();
        // Registered before the links: registering it over existing links runs one full recompute per link, which at
        // 1000 nodes takes minutes.
        diagram.RegisterBehavior(new LinkHalosBehavior(diagram));
        var nodes = GridLayout.AddNodes(diagram, Nodes);
        GridLayout.AddLinks(diagram, nodes);
        _node = nodes[Nodes / 2];
    }

    [Benchmark]
    public void MoveOneNode()
    {
        _moved = !_moved;
        _node.SetPosition(_node.Position.X + (_moved ? 10 : -10), _node.Position.Y);
    }
}
