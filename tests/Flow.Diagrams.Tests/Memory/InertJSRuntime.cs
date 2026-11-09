using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Flow.Diagrams.Tests.Memory;

/// <summary>
/// A <see cref="JSRuntime"/> with no browser behind it: every call succeeds at once, measuring calls answer an 800×600
/// box. Unlike bUnit's JSInterop, which logs every call with its arguments for the context's lifetime (so each
/// <see cref="DotNetObjectReference{TValue}"/> passed to <c>observe</c> keeps its component alive by design), this keeps
/// only what the real runtime keeps: the references it tracks until they are disposed.
/// </summary>
internal sealed class InertJSRuntime : JSRuntime
{
    protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType,
        long targetInstanceId)
    {
        var result = identifier == "FlowDiagrams.getBoundingClientRect"
            ? """{"left":0,"top":0,"right":800,"bottom":600}"""
            : "null";
        DotNetDispatcher.EndInvokeJS(this, $"[{taskId},true,{result}]");
    }

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
    }
}
