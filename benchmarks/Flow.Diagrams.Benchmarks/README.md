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

### Build after the allocation fixes

The tracker reuses its buffers between passes, suspensions are structs, and geometry propagation walks lists by index. Build only, short job, two runs on the same machine; the "before" column is `b4c9a4a` from the table above.

| Nodes | Before mean (error) | After run 1 (error) | After run 2 (error) | Before alloc | After alloc |
|---|---|---|---|---|---|
| 50 | 746.5 us (43.3 us) | 930.6 us (4,208.0 us) | 755.9 us (32.0 us) | 588 KB | 590 KB |
| 200 | 4,096 us (640 us) | 4,642 us (2,427 us) | 4,374 us (470 us) | 2,622 KB | 2,626 KB |
| 1000 | 51.5 ms (28.6 ms) | 51.7 ms (6.4 ms) | 50.7 ms (17.1 ms) | 13,791 KB | 13,797-13,914 KB |

Allocation is back to the base within 1%. Time: the run with tight error bars (run 2) is +1.3% at 50 nodes, +6.8% at 200 and -1.6% at 1000; run 1 is too noisy to read. Every time difference is smaller than the error bars (the 200-node gap is 278 us against errors of 640 and 470 us), so the time is indistinguishable from the base at this precision; a 200-node slowdown of a few percent is not ruled out.
