- `LinkHalosBehavior` now also updates the halos when a link is hidden or shown (including by virtualization), when a
  link switches between straight and smooth paths (at the link's next `Refresh`, as its drawing), and when re-sorting
  the drawing order changes the place of links that share a layer and an order. Before, those left stale halos until
  something below the link changed.
