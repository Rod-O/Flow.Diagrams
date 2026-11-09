using System;
using System.Threading.Tasks;
using Flow.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Flow.Diagrams.Extensions;

/// <summary>Calls into the library's <c>FlowDiagrams</c> JavaScript module (<c>getBoundingClientRect</c>, <c>observe</c>, <c>unobserve</c>), which the host page must load.</summary>
public static class JSRuntimeExtensions
{
    /// <summary>The element's bounding box in screen pixels, relative to the viewport; it is not divided by the diagram's zoom.</summary>
    public static async Task<Rectangle> GetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<Rectangle>("FlowDiagrams.getBoundingClientRect", element);
    }

    /// <summary>
    /// Starts a resize observer on <paramref name="element"/> that calls the <c>OnResize</c> method of <paramref name="reference"/>'s
    /// target whenever the element's size changes. A reference that has already been disposed is ignored.
    /// </summary>
    public static async Task ObserveResizes<T>(this IJSRuntime jsRuntime, ElementReference element,
        DotNetObjectReference<T> reference) where T : class
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("FlowDiagrams.observe", element, reference, element.Id);
        }
        catch (ObjectDisposedException)
        {
            // Ignore, DotNetObjectReference was likely disposed
        }
    }

    /// <summary>Stops the resize observer started by <see cref="ObserveResizes{T}"/> for <paramref name="element"/>.</summary>
    public static async Task UnobserveResizes(this IJSRuntime jsRuntime, ElementReference element)
    {
        await jsRuntime.InvokeVoidAsync("FlowDiagrams.unobserve", element, element.Id);
    }
}