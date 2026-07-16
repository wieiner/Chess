# Arbitrary Small-Cube Solver

P4L includes an owned bounded arbitrary-state solver for 2x2. It accepts only a
state that passes the full 2x2 inventory/orientation validation kernel and uses
deterministic iterative-deepening search over the managed facelet move model.
Time, depth, cancellation, and a memory-derived node limit are enforced.

The managed move model is checked against the native engine for every outer
axis/layer/quarter-turn combination on both 2x2 and 3x3. Known legal 2x2
scrambles plus deterministic seeded native scrambles are solved from imported
facelets without trusted history, and every returned sequence is independently
replayed through a fresh native handle to the solved hash. Impossible
single-corner twists are rejected before search.

This backend intentionally reports maximum size 2. Arbitrary 3x3 remains
deferred until an independently implemented or explicitly licensed two-phase
backend, table lifecycle, and resource packaging are available. Consequently,
P4L does not claim arbitrary 3x3 or arbitrary 11x11 solving.
