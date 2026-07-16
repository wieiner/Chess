# Rubik Physical Solvability Validation

The validator reports separate booleans for basic counts, cubie inventory,
orientation, permutation, known parity constraints, whether parity was actually
proved, solver readiness, and an explicit validation level.

## Proven small-cube kernel

For 2x2, the current kernel proves corner inventory and corner orientation sum.
For a canonical-center 3x3 it additionally proves edge flip sum and equality of
corner/edge permutation parity. Canonical outer-face scrambles pass. Fixtures
with one twisted corner, one flipped edge, or two swapped corners fail the
corresponding invariant.

The native engine also permits direct inner/whole-layer turns. These can move
the observed 3x3 center frame. Until that frame is normalized, the validator
returns `orientationProven=false`/`parityProven=false` instead of incorrectly
applying fixed-center parity equations.

## NxN boundary

For N greater than 3, the validator currently proves basic counts and
corner/wing/center orbit inventory. It does not claim a normalized orientation
frame or a full interchangeable-wing/center permutation proof. Such results
expose `orientationProven=false`, `parityProven=false`, `solverReady=false`, and
a `CubieInventory` level with explicit warnings.

This distinction is intentional: a legal even-cube reduction-parity case is
not declared impossible, while a solved 11x11 is not falsely advertised as
ready for an arbitrary solver. Full NxN parity evolves with the reduction
framework in later phases.
