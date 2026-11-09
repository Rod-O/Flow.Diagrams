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
| `LinkHalosRegisterBenchmarks` | `new LinkHalosBehavior(diagram)` on the grid with its straight links already added |
| `VirtualizationBenchmarks` | `CheckVisibility()` with nothing to change; a pan that moves the 1200×800 viewport between two regions |
| `SvgExportBenchmarks` | `DiagramSvgExporter.ExportAsync` with SVG nodes |

## Results

Intel Xeon W-3245 3.20 GHz, macOS 26.7, .NET SDK 10.0.400, runtime 10.0.11, BenchmarkDotNet
0.15.8, `--job short`. Mean time per operation and bytes allocated per operation. The ObstacleRouter and LinkHalos rows
were measured again after the work described below (macOS 26.7.2); the others are from the first run.

| Benchmark | 50 nodes | 200 nodes | 1000 nodes |
|---|---:|---:|---:|
| Build | 0.74 ms, 585 KB | 4.1 ms, 2.6 MB | 50.7 ms, 13.7 MB |
| ObstacleRouter.RouteAll | 36 ms, 17.7 MB | 375 ms, 201 MB | 8.7 s, 4.5 GB |
| OrthogonalRouter, every link | 4.1 ms, 4.4 MB | 19.6 ms, 20 MB | 107 ms, 109 MB |
| LinkHalos, move one node | 8.6 µs, 12 KB | 25.6 µs, 12.2 KB | 99.8 µs, 12.2 KB |
| LinkHalos, register on existing links | 65 µs, 107 KB | 0.73 ms, 495 KB | 13.9 ms, 2.7 MB |
| Virtualization, CheckVisibility | 1.1 µs, 3.3 KB | 4.2 µs, 12.7 KB | 19.8 µs, 63 KB |
| Virtualization, pan | 1.4 µs, 3.3 KB | 4.9 µs, 12.7 KB | 22.0 µs, 63 KB |
| SVG export | 1.1 ms, 1.2 MB | 8.2 ms, 5.6 MB | 80 ms, 34 MB |

### Before and after the halo and routing work

Same machine and settings; the before column is the code before `LinkHalosBehavior` recomputed only what a change
can reach and `ObstacleRouter` priced each grid step against the placed segments near it.

| Benchmark | Nodes | Before | After |
|---|---:|---:|---:|
| ObstacleRouter.RouteAll | 50 | 283 ms, 54 MB | 36 ms, 17.7 MB |
| ObstacleRouter.RouteAll | 200 | 5.15 s, 572 MB | 375 ms, 201 MB |
| ObstacleRouter.RouteAll | 1000 | about 4.3 min (one cold run) | 8.7 s, 4.5 GB |
| LinkHalos, move one node | 50 | 5.3 ms, 11.1 MB | 8.6 µs, 12 KB |
| LinkHalos, move one node | 200 | 114 ms, 235 MB | 25.6 µs, 12.2 KB |
| LinkHalos, move one node | 1000 | 4.23 s, 6.8 GB | 99.8 µs, 12.2 KB |
| LinkHalos, register on existing links | 50 | 95 ms, 197 MB | 65 µs, 107 KB |
| LinkHalos, register on existing links | 200 | 8.9 s, 18.7 GB | 0.73 ms, 495 KB |
| LinkHalos, register on existing links | 1000 | more than 4 min (stopped) | 13.9 ms, 2.7 MB |

What the numbers say:

- `LinkHalosBehavior` keeps each link's polyline and bounding box; a changed link recomputes itself and the links drawn
  above it whose boxes overlap its old or new box, and a pair of links is tested segment by segment only when their
  boxes meet. A move now costs in proportion to the links near it, plus one pass over the links' boxes.
- `ObstacleRouter.RouteAll` grows about 10× from 50 to 200 nodes and 23× from 200 to 1000. The search itself is no
  longer the cost: each route still collects every other link's segments, indexes them and lays the grid over every
  node, which is linear in the diagram for each of the about 8 500 routes at 1000 nodes. Sharing that work across one
  `RouteAll` pass is the next step if it is needed.
- An export disposes one node renderer per node, and each calls `FlowDiagrams.unobserve` on the app's `IJSRuntime`.
