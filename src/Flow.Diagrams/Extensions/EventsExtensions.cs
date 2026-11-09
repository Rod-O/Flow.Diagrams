using Flow.Diagrams.Core.Events;
using MouseEventArgs = Microsoft.AspNetCore.Components.Web.MouseEventArgs;

namespace Flow.Diagrams.Extensions;

/// <summary>Converts Blazor's DOM event arguments to the framework-independent event arguments of <c>Flow.Diagrams.Core</c>.</summary>
public static class EventsExtensions
{
    /// <summary>Copies a pointer event, including pointer id, size, pressure, tilt and type.</summary>
    public static PointerEventArgs ToCore(this Microsoft.AspNetCore.Components.Web.PointerEventArgs e)
    {
        return new PointerEventArgs(e.ClientX, e.ClientY, e.Button, e.Buttons, e.CtrlKey, e.ShiftKey, e.AltKey,
            e.PointerId, e.Width, e.Height, e.Pressure, e.TiltX, e.TiltY, e.PointerType, e.IsPrimary);
    }

    /// <summary>Copies a mouse event as a pointer event; the pointer-only fields (id, size, pressure, tilt, type) are zero or empty and <c>IsPrimary</c> is <c>false</c>.</summary>
    public static PointerEventArgs ToCore(this MouseEventArgs e)
    {
        return new PointerEventArgs(e.ClientX, e.ClientY, e.Button, e.Buttons, e.CtrlKey, e.ShiftKey, e.AltKey,
            0, 0, 0, 0, 0, 0, string.Empty, false);
    }

    /// <summary>Copies a keyboard event.</summary>
    public static KeyboardEventArgs ToCore(this Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        return new KeyboardEventArgs(e.Key, e.Code, e.Location, e.CtrlKey, e.ShiftKey, e.AltKey);
    }

    /// <summary>Copies a wheel event, including the three deltas and the delta mode.</summary>
    public static WheelEventArgs ToCore(this Microsoft.AspNetCore.Components.Web.WheelEventArgs e)
    {
        return new WheelEventArgs(e.ClientX, e.ClientY, e.Button, e.Buttons, e.CtrlKey, e.ShiftKey, e.AltKey, e.DeltaX,
            e.DeltaY, e.DeltaZ, e.DeltaMode);
    }
}