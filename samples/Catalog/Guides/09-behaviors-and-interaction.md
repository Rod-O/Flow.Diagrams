# Behaviors and interaction

Change what the pointer and keyboard do on the canvas: add your own behavior, remove or replace a built-in one, bind keyboard shortcuts, and put handles (controls) next to models.

{{example custom-behavior}}

## What a behavior is

A [Behavior](xref:Flow.Diagrams.Core.Behavior) is a class that subscribes to the diagram's events in its constructor and unsubscribes in `Dispose`. The canvas only forwards raw events (pointer down, move and up, key down, wheel) to the diagram; everything that happens in response, from selecting a node to panning, is a behavior. The one above adds a node where you double-click the empty canvas:

{{source custom-behavior CustomBehaviorAddNode.cs behavior}}

Register it once the diagram exists:

{{source custom-behavior CustomBehavior.razor register}}

The diagram keeps one behavior per exact type. `RegisterBehavior` throws if that type is already registered, `GetBehavior<T>()` returns the instance of exactly `T`, else a registered subclass of `T`, else `null`, and `UnregisterBehavior<T>()` calls `Dispose` on that same instance and removes it:

{{source custom-behavior CustomBehavior.razor toggle}}

## Registered by default

`new BlazorDiagram()` registers these, in this order. Pass `registerDefaultBehaviors: false` to the constructor to start with none.

| Behavior | What it does |
|---|---|
| [SelectionBehavior](xref:Flow.Diagrams.Core.Behaviors.SelectionBehavior) | Pointer down selects the model under it. Ctrl adds or removes it (if `AllowMultiSelection`); the empty canvas clears the selection. |
| [DragMovablesBehavior](xref:Flow.Diagrams.Core.Behaviors.DragMovablesBehavior) | Drags the selected nodes and groups, snapping to `GridSize`. |
| [DragNewLinkBehavior](xref:Flow.Diagrams.Core.Behaviors.DragNewLinkBehavior) | Dragging from a port draws a new link. |
| [PanBehavior](xref:Flow.Diagrams.Core.Behaviors.PanBehavior) | Dragging the empty canvas pans, unless Shift is held or `AllowPanning` is off. |
| [ZoomBehavior](xref:Flow.Diagrams.Core.Behaviors.ZoomBehavior) | The wheel zooms around the pointer, within `Options.Zoom`. |
| [EventsBehavior](xref:Flow.Diagrams.Core.Behaviors.EventsBehavior) | Turns pointer down and up into `PointerClick` and `PointerDoubleClick`. |
| [KeyboardShortcutsBehavior](xref:Flow.Diagrams.Core.Behaviors.KeyboardShortcutsBehavior) | Runs actions on key combinations. |
| [ControlsBehavior](xref:Flow.Diagrams.Core.Controls.ControlsBehavior) | Shows and hides controls on selection or hover. |
| [VirtualizationBehavior](xref:Flow.Diagrams.Core.Behaviors.VirtualizationBehavior) | Hides models outside the view; inactive until you enable virtualization (see [Performance](guides/performance)). |

[LinkHalosBehavior](xref:Flow.Diagrams.Core.Behaviors.LinkHalosBehavior) (new in Flow.Diagrams) and [DebugEventsBehavior](xref:Flow.Diagrams.Core.Behaviors.DebugEventsBehavior) exist but are not registered; add them yourself.

To turn something off, prefer its option when there is one (`AllowPanning`, `Options.Zoom.Enabled`, `AllowMultiSelection`) and unregister the behavior otherwise. Behaviors handle each event in registration order, so a behavior you add runs after the built-in ones.

**Replacing a built-in behavior.** Unregister it and register your class; a subclass keeps the built-in lookups working, since `GetBehavior<DragNewLinkBehavior>()` finds it. `UnregisterBehavior<DragNewLinkBehavior>()` removes it too. The built-in `LinkWidget`, `DragNewLinkControl` and `ArrowHeadControl` look up `DragNewLinkBehavior`, and the two controls throw a `DiagramsException` without it.

## Clicks and double clicks

`PointerClick` fires on pointer up when the pointer did not move since pointer down on the same model. `PointerDoubleClick` fires right after a click that comes within 500 ms of the previous click on the same model (two clicks on the background count as the same). The click that completes a double click starts no new one, so three quick clicks are one double click and one click. Both events need `EventsBehavior`; without it they never fire.

Double-clicking a link vertex removes the vertex and raises the link's `VertexRemoved`, not a diagram event.

## Keyboard shortcuts

`KeyboardShortcutsBehavior` starts with `Delete` (delete the selection, honouring `Locked` and `Options.Constraints`) and `Ctrl+Alt+g` (group or ungroup the selected nodes, if `Options.Groups.Enabled`). Bind your own with `SetShortcut`, remove one with `RemoveShortcut`; the actions are in [KeyboardShortcutsDefaults](xref:Flow.Diagrams.Core.Behaviors.KeyboardShortcutsDefaults) so you can bind them to other keys.

{{source custom-behavior CustomBehavior.razor shortcut}}

The key is the browser's `KeyboardEvent.key`, compared ignoring case, so Ctrl+Shift+A is `SetShortcut("a", true, true, false, ...)` even though the browser reports `"A"`. When the key types something other than a letter or digit, as Option does on macOS (Option+G is `©`), the physical key is used instead, so the default `Ctrl+Alt+g` works there too. The overload with a `meta` argument binds shortcuts that need Meta (Cmd on macOS); the others fire only when Meta is not held. If an action throws, `ActionFailed` receives the exception; without a handler it is rethrown and reaches Blazor as an unhandled error.

Traps:

- Keys reach the diagram only while the canvas has focus. Clicking it focuses it.

## Selection

Click selects, Ctrl-click adds or removes. For rubber-band selection put a [SelectionBoxWidget](xref:Flow.Diagrams.Components.Widgets.SelectionBoxWidget) in the canvas's `Widgets`; it is a widget, not a behavior. Shift-drag on the empty canvas draws the box and selects every node it overlaps. It selects **nodes only**: groups and links inside the box are not selected. Listen to `Diagram.SelectionChanged` to react; it fires once per model whose selection changed.

## Controls

A control is a small handle drawn next to one model: a remove button, a frame, a draggable arrowhead. Select a node or a link below.

{{example controls}}

Each model gets a [ControlsContainer](xref:Flow.Diagrams.Core.Controls.ControlsContainer) from `Diagram.Controls.AddFor(model, type)`. `ControlsType.OnSelection` (the default) shows the controls while the model is selected, `OnHover` while the pointer is over it.

{{source controls ControlsExample.cs node}}

{{source controls Controls.razor link}}

The built-in controls, in `Flow.Diagrams.Core.Controls.Default`:

| Control | Effect |
|---|---|
| [RemoveControl](xref:Flow.Diagrams.Core.Controls.Default.RemoveControl) | Deletes the model, honouring `Locked` and the `ShouldDelete*` constraints. Placed at fractions of the bounds (`1, 0` is the top-right corner) or by any `IPositionProvider`. |
| [BoundaryControl](xref:Flow.Diagrams.Core.Controls.Default.BoundaryControl) | A dashed frame 10 units outside the model. |
| [DragNewLinkControl](xref:Flow.Diagrams.Core.Controls.Default.DragNewLinkControl) | Starts a new link from a node. |
| [ArrowHeadControl](xref:Flow.Diagrams.Core.Controls.Default.ArrowHeadControl) | A draggable arrowhead at one end of a link, to reconnect that end; the other end stays attached. Dropping it raises `TargetAttached`, or `SourceAttached` for the source end. |

A control of your own is a [Control](xref:Flow.Diagrams.Core.Controls.Control) (it only has a position) or an [ExecutableControl](xref:Flow.Diagrams.Core.Controls.ExecutableControl) (it also acts on pointer down), plus a component that draws it:

{{source controls ControlsAddChild.cs control}}

{{source controls ControlsAddChildWidget.razor widget}}

{{source controls Controls.razor register}}

The component receives the control as `Control` and the model as `Model`. It is drawn in the model's layer: SVG markup for SVG nodes, SVG groups and links, HTML for the rest.

Traps:

- Controls are added per model. Links the user draws, and nodes you add later, have none until you call `AddFor` for them (for example in `Diagram.Links.Added`). `AddFor` returns the existing container if the model has one, so adding the same controls again duplicates them.
- With `OnHover`, a control drawn outside the model's element is not inside it: moving the pointer onto the control leaves the model and hides the control. Use `OnSelection` for buttons placed outside the shape.
- Removing a model removes its controls.
