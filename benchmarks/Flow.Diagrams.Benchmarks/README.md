# Flow.Diagrams benchmarks

Run: `dotnet run -c Release --project benchmarks/Flow.Diagrams.Benchmarks -- --filter "*BuildBenchmarks*" "*ObstacleRouterBenchmarks*"`

## Change tracking (2026-11-09)

`origin/master` (`b4c9a4a`) against the `change-tracking` branch. Short job (`--job short`), one run each, macOS arm, so the error bars are wide; the allocation column is exact. There is no drag benchmark; `ObstacleRouterBenchmarks.RouteAll` is the routing one.

| Benchmark | Nodes | Before mean (error) | After mean (error) | Change | Before alloc | After alloc |
|---|---|---|---|---|---|---|
| `BuildBenchmarks.Build` | 50 | 746.5 us (43.3 us) | 782.4 us (194.5 us) | +4.8% | 588 KB | 646 KB |
| `BuildBenchmarks.Build` | 200 | 4,096 us (640 us) | 4,507 us (1,009 us) | +10.0% | 2,622 KB | 2,879 KB |
| `BuildBenchmarks.Build` | 1000 | 51.5 ms (28.6 ms) | 53.2 ms (25.6 ms) | +3.3% | 13,791 KB | 15,267 KB |
| `ObstacleRouterBenchmarks.RouteAll` | 50 | 35.15 ms (1.4 ms) | 34.78 ms (1.8 ms) | -1.1% | 17.69 MB | 17.71 MB |
| `ObstacleRouterBenchmarks.RouteAll` | 200 | 380.4 ms (81.6 ms) | 370.7 ms (109.0 ms) | -2.6% | 201.22 MB | 201.31 MB |
| `ObstacleRouterBenchmarks.RouteAll` | 1000 | 8,676 ms (315 ms) | 9,128 ms (4,320 ms) | +5.2% | 4,537 MB | 4,537 MB |

Build is 3-10% slower and allocates about 10% more at every size; RouteAll is within noise (its 1000-node error is 4.3 s). One short run each, so not conclusive.
