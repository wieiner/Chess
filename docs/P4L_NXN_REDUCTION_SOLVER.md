# NxN Reduction Solver Framework

The reduction framework models seven ordered phases: orientation normalization,
center solving, wing pairing, reduced-3x3 construction, reduced-3x3 solving,
parity correction, and independent solved verification. Every phase names its
entry and exit invariant.

For 4x4, 5x5, and 7x7 legal states, the current implementation validates the
portable state and cubie inventory, creates a deterministic guided plan, and
produces a bounded resumable checkpoint tied to solver id, schema version,
size, and input hash. Checkpoints reject changed inputs and invalid moves.

The result status is deliberately `Incomplete`: center/wing move generation,
reduced-3x3 hand-off, and parity algorithms are not implemented, and the
checkpoint contains zero emitted moves. This is the state-machine foundation,
not a claim that arbitrary NxN states are solved. Phase 26 applies this honest
Level A boundary to 11x11.
