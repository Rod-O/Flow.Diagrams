using BenchmarkDotNet.Attributes;
using Flow.Diagrams.Core.Behaviors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Options;

namespace Flow.Diagrams.Benchmarks;

/// <summary>
/// <see cref="VirtualizationBehavior.CheckVisibility()"/> on a 1200×800 viewport over the grid: once with nothing to
/// change, and through a pan that moves the viewport back and forth between the grid's corner and its middle.
/// </summary>
[MemoryDiagnoser]
public class VirtualizationBenchmarks
{
    private BlazorDiagram _diagram = null!;
    private VirtualizationBehavior _behavior = null!;
    private bool _panned;

    [Params(50, 200, 1000)] public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _diagram = new BlazorDiagram(new BlazorDiagramOptions { Virtualization = { Enabled = true } });
        _diagram.SetContainer(new Rectangle(0, 0, 1200, 800));
        GridLayout.AddLinks(_diagram, GridLayout.AddNodes(_diagram, Nodes));
        _behavior = _diagram.GetBehavior<VirtualizationBehavior>()!;
    }

    [Benchmark]
    public void CheckVisibilityUnchanged() => _behavior.CheckVisibility();

    [Benchmark]
    public void PanInAndOut()
    {
        _panned = !_panned;
        var offset = _panned ? -GridLayout.Pitch * GridLayout.Columns(Nodes) / 2 : 0;
        _diagram.SetPan(offset, offset);
    }
}
