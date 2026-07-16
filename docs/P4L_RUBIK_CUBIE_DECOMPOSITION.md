# Rubik Facelet-to-Cubie Decomposition

`RubikCubieDecomposer` reads the canonical U/R/F/D/L/B facelet shell and
enumerates physical surface coordinates. Three exposed faces form a corner,
two form a wing/edge observation, and one forms a center observation.

Corners store their color triple and observable orientation. Wings store their
color pair, current coordinate, free-axis index, reflection-invariant orbit,
and observable flip. This is important on NxN cubes: several physical wings
share the same pair, so pair alone is never promoted to an invented unique ID.
Centers store color and a rotation-invariant face-distance orbit.

Completeness compares corner signatures, wing pair+orbit multisets, and center
color+orbit multisets against solved topology. Invalid or ambiguous inventory
returns structured issues while the original facelet document remains usable;
no native state is mutated and no cubie identity is fabricated.

Contracts cover solved and legal native scrambles for N=2,3,4,5,8,11,
duplicate/impossible corners, and count-preserving center-orbit corruption.
This phase proves inventory decomposition, not full permutation/parity
solvability. Phase 19 owns those stronger claims.
