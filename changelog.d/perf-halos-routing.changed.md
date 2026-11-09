- `LinkHalosBehavior` recomputes only the links whose bounding boxes overlap the one that changed, instead of every link
  drawn above it, and registering it on a diagram that already has links computes the halos once instead of once per
  link. Moving one node of a 1000-node grid (2842 straight links) went from 4.2 s and 6.8 GB allocated to 0.16 ms and
  20 KB; registering on it, from more than four minutes to 23 ms. The halos are the same.
