# Benchmarks

Time and allocations of the operations whose cost grows with the diagram, measured with
[BenchmarkDotNet](https://benchmarkdotnet.org/). They measure; they are not a gate, and CI does not run them.

```
dotnet run -c Release --project benchmarks/Flow.Diagrams.Benchmarks -- --filter '*'
```

`--filter '*Halos*'` runs one class; `--job short` (3 iterations) is what the table below used.

Every benchmark uses the same diagram (`GridLayout`): nodes of 100×60 on a square grid with a 200 pitch, a measured
port on each side, each node linked to its right neighbour, and two crossing diagonals in each cell.

| Nodes | Links |
|------:|------:|
| 50 | 115 |
| 200 | 530 |
| 1000 | 2842 |

| Class | What one operation is |
|---|---|
| `BuildBenchmarks` | `new BlazorDiagram()` (default behaviors), then adding the nodes and links, routed by the defaults |
| `ObstacleRouterBenchmarks` | `ObstacleRouter.RouteAll` (two re-route passes) |
| `OrthogonalRouterBenchmarks` | `OrthogonalRouter.GetRoute` for every link |
| `LinkHalosBenchmarks` | moving the middle node by 10 with `LinkHalosBehavior` and straight links |
| `VirtualizationBenchmarks` | `CheckVisibility()` with nothing to change; a pan that moves the 1200×800 viewport between two regions |
| `SvgExportBenchmarks` | `DiagramSvgExporter.ExportAsync` with SVG nodes |

## Results

Intel Xeon W-3245 3.20 GHz, macOS 26.7, .NET SDK 10.0.400, runtime 10.0.11, BenchmarkDotNet
0.15.8, `--job short`. Mean time per operation and bytes allocated per operation.

| Benchmark | 50 nodes | 200 nodes | 1000 nodes |
|---|---:|---:|---:|
| Build | 0.74 ms, 585 KB | 4.1 ms, 2.6 MB | 50.7 ms, 13.7 MB |
| ObstacleRouter.RouteAll | 278 ms, 54 MB | 5.19 s, 572 MB | about 4.3 min (one cold run; not in the suite) |
| OrthogonalRouter, every link | 4.1 ms, 4.4 MB | 19.6 ms, 20 MB | 107 ms, 109 MB |
| LinkHalos, move one node | 5.4 ms, 11 MB | 116 ms, 235 MB | 4.23 s, 6.8 GB |
| Virtualization, CheckVisibility | 1.1 µs, 3.3 KB | 4.2 µs, 12.7 KB | 19.8 µs, 63 KB |
| Virtualization, pan | 1.4 µs, 3.3 KB | 4.9 µs, 12.7 KB | 22.0 µs, 63 KB |
| SVG export | 1.1 ms, 1.2 MB | 8.2 ms, 5.6 MB | 80 ms, 34 MB |

What the numbers say:

- `ObstacleRouter.RouteAll` grows about 19× from 50 to 200 nodes and is impractical at 1000; it is why the class runs
  only 50 and 200.
- `LinkHalosBehavior` recomputes every link drawn above each refreshed link: one node move costs 21× more from 50 to
  200 nodes and 36× more from 200 to 1000, where it allocates 6.8 GB.
- Registering `LinkHalosBehavior` on a diagram that already has its links recomputes once per link: its constructor
  calls `UpdateFrom(link)` for each link before `UpdateAll()`. At 1000 nodes it ran for more than four minutes before
  being stopped, so the benchmark registers the behavior before adding the links.
- An export disposes one node renderer per node, and each calls `FlowDiagrams.unobserve` on the app's `IJSRuntime`.
