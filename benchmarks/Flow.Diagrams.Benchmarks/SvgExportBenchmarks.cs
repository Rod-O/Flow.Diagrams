using BenchmarkDotNet.Attributes;
using Flow.Diagrams.Export;
using Flow.Diagrams.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Flow.Diagrams.Benchmarks;

/// <summary><see cref="DiagramSvgExporter.ExportAsync"/> of the grid drawn with SVG nodes.</summary>
[MemoryDiagnoser]
public class SvgExportBenchmarks
{
    private BlazorDiagram _diagram = null!;
    private ServiceProvider _services = null!;

    [Params(50, 200, 1000)] public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _diagram = new BlazorDiagram();
        GridLayout.AddLinks(_diagram, GridLayout.AddNodes(_diagram, Nodes, p => new SvgNodeModel(p)));
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IJSRuntime, NoJSRuntime>()
            .BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup() => _services.Dispose();

    [Benchmark]
    public async Task<int> Export() => (await DiagramSvgExporter.ExportAsync(_diagram, _services)).Svg.Length;

    /// <summary>
    /// The renderers inject a runtime, and each node renderer calls <c>FlowDiagrams.unobserve</c> on it when the export's
    /// renderer disposes it; this one answers every call at once.
    /// </summary>
    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);
    }
}
