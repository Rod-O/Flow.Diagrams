using Flow.Diagrams.Core;
using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Routers;

namespace Flow.Diagrams.Benchmarks;

/// <summary>
/// The diagram every benchmark measures: nodes on a square grid, each with a measured port on every side, joined to
/// their right neighbour and, across each cell, by two diagonals that cross. About three links per node.
/// </summary>
internal static class GridLayout
{
    public const double NodeWidth = 100;
    public const double NodeHeight = 60;
    public const double Pitch = 200;
    private const double PortSize = 10;

    public static int Columns(int count) => (int)Math.Ceiling(Math.Sqrt(count));

    public static NodeModel[] AddNodes(Diagram diagram, int count, Func<Point, NodeModel>? create = null)
    {
        create ??= p => new NodeModel(p);
        var columns = Columns(count);
        var nodes = new NodeModel[count];
        for (var i = 0; i < count; i++)
        {
            var node = create(new Point(i % columns * Pitch, i / columns * Pitch));
            node.Size = new Size(NodeWidth, NodeHeight);
            foreach (var alignment in new[] { PortAlignment.Top, PortAlignment.Right, PortAlignment.Bottom, PortAlignment.Left })
                AddMeasuredPort(node, alignment);
            nodes[i] = diagram.Nodes.Add(node);
        }

        return nodes;
    }

    public static List<LinkModel> AddLinks(Diagram diagram, NodeModel[] nodes, Router? router = null)
    {
        var columns = Columns(nodes.Length);
        var links = new List<LinkModel>();
        void Link(NodeModel from, PortAlignment fromSide, NodeModel to, PortAlignment toSide) =>
            links.Add(diagram.Links.Add(new LinkModel(new SinglePortAnchor(Port(from, fromSide)), new SinglePortAnchor(Port(to, toSide)))
            {
                Router = router,
            }));

        for (var i = 0; i < nodes.Length; i++)
        {
            var right = i % columns < columns - 1 && i + 1 < nodes.Length;
            if (right)
                Link(nodes[i], PortAlignment.Right, nodes[i + 1], PortAlignment.Left);
            if (right && i + columns + 1 < nodes.Length)
            {
                Link(nodes[i], PortAlignment.Bottom, nodes[i + columns + 1], PortAlignment.Top);
                Link(nodes[i + 1], PortAlignment.Bottom, nodes[i + columns], PortAlignment.Top);
            }
        }

        return links;
    }

    private static PortModel Port(NodeModel node, PortAlignment alignment) => node.Ports.First(p => p.Alignment == alignment);

    private static void AddMeasuredPort(NodeModel node, PortAlignment alignment)
    {
        var b = node.GetBounds()!;
        var centre = alignment switch
        {
            PortAlignment.Top => new Point(b.Left + b.Width / 2, b.Top),
            PortAlignment.Right => new Point(b.Right, b.Top + b.Height / 2),
            PortAlignment.Bottom => new Point(b.Left + b.Width / 2, b.Bottom),
            _ => new Point(b.Left, b.Top + b.Height / 2),
        };
        var port = node.AddPort(new PortModel(node, alignment, new Point(centre.X - PortSize / 2, centre.Y - PortSize / 2),
            new Size(PortSize, PortSize)));
        port.Initialized = true;
    }
}
