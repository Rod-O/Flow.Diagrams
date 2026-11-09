using Flow.Diagrams.Components;
using Flow.Diagrams.Core.Geometry;
using Bunit;
using Xunit;

namespace Flow.Diagrams.Tests.Components
{
    public class DiagramCanvasTests
    {
        [Fact]
        public void Behavior_WhenDisposing_ShouldUnsubscribeToResizes()
        {
            // Arrange
            JSRuntimeInvocationHandler call;
            using (var ctx = new TestContext())
            {
                ctx.JSInterop.Setup<Rectangle>("FlowDiagrams.getBoundingClientRect", _ => true);
                call = ctx.JSInterop.SetupVoid("FlowDiagrams.unobserve", _ => true).SetVoidResult();

                // Act
                var cut = ctx.RenderComponent<DiagramCanvas>(p => p.Add(n => n.BlazorDiagram, new BlazorDiagram()));
            }

            // Assert
            call.VerifyInvoke("FlowDiagrams.unobserve", calledTimes: 1);
        }
    }
}
