# Flow.Diagrams benchmarks

Run: `dotnet run -c Release --project benchmarks/Flow.Diagrams.Benchmarks -- --filter "*BuildBenchmarks*" "*ObstacleRouterBenchmarks*"`

## Change tracking (2026-11-09)

`origin/master` (`b4c9a4a`) against the `change-tracking` branch. Short job (`--job short`), one run each, macOS arm, so the error bars are wide; the allocation column is exact. There is no drag benchmark; `ObstacleRouterBenchmarks.RouteAll` is the routing one.

| Benchmark | Nodes | Before (mean) | After (mean) | Before alloc | After alloc |
|---|---|---|---|---|---|
| `BuildBenchmarks.Build` | 50 | 746.5 us | 782.4 us | 588 KB | 646 KB |
| `BuildBenchmarks.Build` | 200 | 4,096 us | 4,507 us | 2,622 KB | 2,879 KB |
| `BuildBenchmarks.Build` | 1000 | 51.5 ms | 53.2 ms | 13,791 KB | 15,267 KB |
| `ObstacleRouterBenchmarks.RouteAll` | 50 | 35.15 ms | 34.78 ms | 17.69 MB | 17.71 MB |
| `ObstacleRouterBenchmarks.RouteAll` | 200 | 380.4 ms | 370.7 ms | 201.22 MB | 201.31 MB |
| `ObstacleRouterBenchmarks.RouteAll` | 1000 | 8,676 ms | 9,128 ms | 4,537 MB | 4,537 MB |

Build allocates about 10% more (change tracker bookkeeping); the means differ within the error.
