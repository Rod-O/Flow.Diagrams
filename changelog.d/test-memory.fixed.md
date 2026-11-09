- `VirtualizationBehavior`: unregistering it left every node and group holding it through `Moved`, so the behavior
  stayed alive and kept rechecking visibility after each drag. `Dispose` now unsubscribes from them.
- `EventsBehavior`: a removed node, link or group stayed reachable from the behavior when it was the last one pressed
  or clicked, until the next press or click. It now forgets a model when the model is removed.
