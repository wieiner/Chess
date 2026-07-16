# Rubik 11x11 Solver Result

## Achieved level: A

A deterministic legal 11x11 scramble is exported and imported through the
portable state format. The imported state passes bounded load, basic counts,
corner/wing/center decomposition, and cubie-orbit inventory validation. The
reduction framework emits the complete ordered guidance plan, including center
solving and wing pairing phases.

The matching checkpoint is saved atomically as
`<name>.solve-checkpoint.json` and resumes only against the same size/input
hash. A `<name>.solution.rubikmoves` artifact can also be saved; at Level A it
contains zero moves and explicitly records `complete=false` and
`verified=false`.

## Not achieved

- Level B: centers are not solved, wings are not paired, and no reduced 3x3 is
  produced.
- Level C: no complete arbitrary 11x11 move sequence exists, so independent
  replay cannot reach and confirm the solved hash.

Reverse history is not used as evidence. Therefore the correct result is
"11x11 reduction Level A", not "arbitrary 11x11 solve PASS".
