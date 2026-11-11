using Flow.Diagrams.Core.Anchors;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.PathGenerators;
using Flow.Diagrams.Core.Routers;
using FluentAssertions;
using Xunit;

namespace Flow.Diagrams.Core.Tests.ChangeTracking;

public class NotifyingPropertiesTests
{
    public record TestDiagramModels(
        TestDiagram Diagram,
        NodeModel Node,
        NodeModel Other,
        PortModel Port,
        PortModel OtherPort,
        LinkModel Link,
        LinkLabelModel Label,
        LinkVertexModel Vertex,
        GroupModel Group);

    public static TheoryData<string, Action<TestDiagramModels>, ModelChanges> Setters => new()
    {
        { "Locked", m => m.Node.Locked = true, ModelChanges.Appearance },
        { "Selected", m => m.Node.Selected = true, ModelChanges.Appearance },
        { "Position", m => m.Node.Position = new Point(5, 5), ModelChanges.Geometry },
        { "Title", m => m.Node.Title = "t", ModelChanges.Appearance },
        { "Size", m => m.Node.Size = new Size(10, 10), ModelChanges.Geometry },
        { "Port.Position", m => m.Port.Position = new Point(7, 7), ModelChanges.Geometry },
        { "Port.Size", m => m.Port.Size = new Size(4, 4), ModelChanges.Geometry },
        { "Router", m => m.Link.Router = new NormalRouter(), ModelChanges.Geometry },
        { "PathGenerator", m => m.Link.PathGenerator = new StraightPathGenerator(), ModelChanges.Geometry },
        { "SourceMarker", m => m.Link.SourceMarker = LinkMarker.Arrow, ModelChanges.Geometry },
        { "TargetMarker", m => m.Link.TargetMarker = LinkMarker.Arrow, ModelChanges.Geometry },
        { "SetSource", m => m.Link.SetSource(new SinglePortAnchor(m.OtherPort)), ModelChanges.Geometry },
        { "SetTarget", m => m.Link.SetTarget(new SinglePortAnchor(m.Port)), ModelChanges.Geometry },
        { "AddVertex", m => m.Link.AddVertex(new Point(1, 1)), ModelChanges.Geometry },
        { "Segmentable", m => m.Link.Segmentable = true, ModelChanges.Appearance },
        { "AddLabel", m => m.Link.AddLabel("x"), ModelChanges.Appearance },
        { "Color", m => m.Link.Color = "red", ModelChanges.Appearance },
        { "SelectedColor", m => m.Link.SelectedColor = "red", ModelChanges.Appearance },
        { "Width", m => m.Link.Width = 5, ModelChanges.Appearance },
        { "Label.Content", m => m.Label.Content = "y", ModelChanges.Appearance },
        { "Label.Distance", m => m.Label.Distance = 0.3, ModelChanges.Appearance },
        { "Label.Offset", m => m.Label.Offset = new Point(2, 2), ModelChanges.Appearance },
    };

    [Theory, MemberData(nameof(Setters))]
    public void Setter_ShouldMarkItsModel(string member, Action<TestDiagramModels> set, ModelChanges expected)
    {
        // Arrange
        var models = Create();
        var target = Marked(member, models);
        var raised = new List<ModelChanges>();
        target.Changed += (_, changes) => raised.Add(changes);

        // Act
        set(models);

        // Assert
        raised.Should().HaveCount(1, member);
        raised[0].HasFlag(expected).Should().BeTrue(member);
    }

    [Fact]
    public void SettingTheSameValue_ShouldNotRaise()
    {
        // Arrange
        var models = Create();
        models.Node.Title = "a";
        var raised = 0;
        models.Node.Changed += (_, _) => raised++;

        // Act
        models.Node.Title = "a";

        // Assert
        raised.Should().Be(0);
    }

    [Fact]
    public void ChangingAPropertyOutsideABatch_ShouldRaiseAtOnce()
    {
        // Arrange
        var models = Create();
        var raised = 0;
        models.Node.Changed += (_, _) => raised++;

        // Act
        models.Node.Title = "b";

        // Assert
        raised.Should().Be(1);
    }

    [Fact]
    public void SetPosition_ShouldRouteEachAttachedLinkOnce()
    {
        // Arrange
        var models = Create();
        var router = new CountingRouter();
        models.Link.Router = router;
        router.Calls = 0;

        // Act
        models.Node.SetPosition(10, 10);

        // Assert
        router.Calls.Should().Be(1);
    }

    [Fact]
    public void GroupSetPosition_ShouldRouteEachAttachedLinkOnceAndMoveTheChildren()
    {
        // Arrange
        var models = Create();
        var router = new CountingRouter();
        models.Link.Router = router;
        router.Calls = 0;
        var childBefore = models.Node.Position;

        // Act
        models.Group.SetPosition(models.Group.Position.X + 50, models.Group.Position.Y + 20);

        // Assert
        models.Node.Position.Should().Be(new Point(childBefore.X + 50, childBefore.Y + 20));
        router.Calls.Should().Be(1);
    }

    [Fact]
    public void GroupSetPosition_OutsideADiagram_ShouldMoveTheGroupWithItsChildren()
    {
        // Arrange
        var child = new NodeModel(new Point(100, 100)) { Size = new Size(50, 50) };
        var group = new GroupModel([child], padding: 10);
        var childBefore = child.Position;
        var groupBefore = group.Position;

        // Act
        group.SetPosition(groupBefore.X + 30, groupBefore.Y + 40);

        // Assert
        child.Position.Should().Be(new Point(childBefore.X + 30, childBefore.Y + 40));
        group.Position.Should().Be(new Point(groupBefore.X + 30, groupBefore.Y + 40));
    }

    // The model whose Changed the member is expected to raise.
    private static Model Marked(string member, TestDiagramModels m) => member switch
    {
        _ when member.StartsWith("Port.") => m.Port,
        "Router" or "PathGenerator" or "SourceMarker" or "TargetMarker" or "SetSource" or "SetTarget" or "AddVertex"
            or "Segmentable" or "AddLabel" or "Color" or "SelectedColor" or "Width" => m.Link,
        _ when member.StartsWith("Label.") => m.Label,
        _ => m.Node
    };

    private static TestDiagramModels Create()
    {
        var diagram = new TestDiagram();
        var node = diagram.Nodes.Add(new NodeModel(new Point(100, 100)) { Size = new Size(100, 50) });
        var other = diagram.Nodes.Add(new NodeModel(new Point(500, 100)) { Size = new Size(100, 50) });
        var port = node.AddPort(PortAlignment.Right);
        var otherPort = other.AddPort(PortAlignment.Left);
        var link = diagram.Links.Add(new LinkModel(port, otherPort));
        var label = link.AddLabel("label");
        var vertex = link.AddVertex(new Point(300, 50));
        var sibling = diagram.Nodes.Add(new NodeModel(new Point(100, 300)) { Size = new Size(100, 50) });
        var group = diagram.Groups.Group(node, sibling);
        return new TestDiagramModels(diagram, node, other, port, otherPort, link, label, vertex, group);
    }

    private sealed class CountingRouter : Router
    {
        public int Calls { get; set; }

        public override Point[] GetRoute(Diagram diagram, BaseLinkModel link)
        {
            Calls++;
            return [];
        }
    }
}
