- `LinkHalosBehavior` recomputes only the links whose bounding boxes overlap the one that changed, instead of every link
  drawn above it, and registering it on a diagram that already has links computes the halos once instead of once per
  link. On a 1000-node grid with 2842 straight links, moving one node went from 4.2 s and 6.8 GB allocated to 0.1 ms
  and 12 KB; registering the behavior, from more than four minutes to 14 ms. The halos are the same.
- `ObstacleRouter` prices each grid step once, against the placed segments near it, instead of against every segment
  of every other link at each step of its search, and reuses its working arrays between routes. `RouteAll` on a
  200-node grid went from 5.2 s to 0.38 s, and on a 1000-node grid from about four minutes to 8.7 s. The routes are
  the same.
