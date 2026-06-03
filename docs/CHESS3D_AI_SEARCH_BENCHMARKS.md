# Chess3D AI Search Benchmarks

P3D.1 exposes lightweight search metrics through summary JSON instead of adding a strict timing benchmark.

Tracked diagnostics:

- profile id;
- requested/completed depth;
- nodes;
- qnodes;
- elapsed milliseconds;
- cutoffs;
- stopped reason;
- best action compact text.

CI must not depend on exact timings. Tests check parseability, positive node counts when candidates exist, and no-mutation invariants.

