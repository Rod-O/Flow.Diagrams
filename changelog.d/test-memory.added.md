- Memory-leak tests (`Category=Memory`) in both test projects: removed nodes, links and groups, unregistered
  behaviors, dropped diagrams, and disposed canvases and components must become collectable.
- `benchmarks/Flow.Diagrams.Benchmarks` (BenchmarkDotNet): time and allocations of building a diagram, `ObstacleRouter`,
  `OrthogonalRouter`, `LinkHalosBehavior`, `VirtualizationBehavior` and `DiagramSvgExporter` at 50, 200 and 1000 nodes.
