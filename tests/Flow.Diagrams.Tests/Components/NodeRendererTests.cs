using Flow.Diagrams.Components.Renderers;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Bunit;
using Xunit;

namespace Flow.Diagrams.Tests.Components;

public class NodeRendererTests
{

    [Fact]
    public void NodeRenderer_WhenNodeVisibleIsFalseAndIsFirstRender_ShouldNotObserveResizes()
    {
        // Arrange
        JSRuntimeInvocationHandler call;
        using (var ctx = new TestContext())
        {
            ctx.JSInterop.Setup<Rectangle>("FlowDiagrams.getBoundingClientRect", _ => true);
            call = ctx.JSInterop.SetupVoid("FlowDiagrams.observe", _ => true).SetVoidResult();

            // Act
            var cut = ctx.RenderComponent<NodeRenderer>(p =>
            {
                p.Add(n => n.BlazorDiagram, new BlazorDiagram());
                p.Add(n => n.Node, new NodeModel()
                {
                    Visible = false
                });
            });
        }

        // Assert
        call.VerifyNotInvoke("FlowDiagrams.observe");
    }
}
