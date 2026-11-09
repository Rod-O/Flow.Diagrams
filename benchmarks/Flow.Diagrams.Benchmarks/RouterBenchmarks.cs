using BenchmarkDotNet.Attributes;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Routers;

namespace Flow.Diagrams.Benchmarks;

/// <summary>
/// <see cref="ObstacleRouter.RouteAll"/> over the grid, with the default two re-route passes.
/// </summary>
[MemoryDiagnoser]
public class ObstacleRouterBenchmarks
{
    private BlazorDiagram _diagram = null!;
    private readonly ObstacleRouter _router = new();

    [Params(50, 200, 1000)] public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Built with the default router so the setup does not pay for routing.
        _diagram = new BlazorDiagram();
        GridLayout.AddLinks(_diagram, GridLayout.AddNodes(_diagram, Nodes));
        _diagram.Options.Links.DefaultRouter = _router;
    }

    [Benchmark]
    public void RouteAll() => _router.RouteAll(_diagram);
}

/// <summary><see cref="OrthogonalRouter.GetRoute"/> for every link of the grid; divide by the link count for one route.</summary>
[MemoryDiagnoser]
public class OrthogonalRouterBenchmarks
{
    private BlazorDiagram _diagram = null!;
    private List<LinkModel> _links = null!;
    private readonly OrthogonalRouter _router = new();

    [Params(50, 200, 1000)] public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _diagram = new BlazorDiagram();
        _links = GridLayout.AddLinks(_diagram, GridLayout.AddNodes(_diagram, Nodes));
    }

    [Benchmark]
    public int EveryLink()
    {
        var points = 0;
        foreach (var link in _links)
            points += _router.GetRoute(_diagram, link).Length;
        return points;
    }
}
