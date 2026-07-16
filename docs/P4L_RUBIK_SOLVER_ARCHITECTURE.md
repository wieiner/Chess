# Rubik Solver Architecture

## Decision

The product uses a solver contract plus independent replay verification. The
first owned arbitrary-state backend is a bounded 2x2 search kernel. The
existing reverse-history feature remains a separate trusted-history solver.
Future 3x3 and NxN backends plug into the same contract; no backend is treated
as successful until its move sequence is replayed from the original facelets
and reaches the canonical solved state.

## Options considered

| Option | Strength | Cost/risk | Decision |
| --- | --- | --- | --- |
| Owned solver | Full control over contracts, bounds, determinism, and licensing | 3x3 tables and NxN reduction require substantial implementation and testing | Use for bounded 2x2 and shared infrastructure |
| Embedded library | Potentially mature algorithms | License, native packaging, table generation, and update ownership | No dependency selected in P4L |
| External executable | Strong process and license isolation | Deployment, protocol, timeout, and trust boundary | Future opt-in adapter only |
| Plugin backend | Stable product API and replaceable solver | Versioning and capability negotiation required | Preferred extension boundary |

The inspected Kociemba Python reference is GPL-3.0 and describes roughly 80 MB
of generated pruning tables. Its algorithm description is useful research, but
its implementation is not copied or embedded. A permissively licensed package
would still require a dependency/license review, deterministic packaging, and
independent verification before adoption.

## Capability tiers

1. `TrustedHistoryReverse`: invert engine-recorded legal history. It rejects a
   manual/imported state and is never called an arbitrary solver.
2. `Arbitrary2x2Bounded`: owned search with explicit time, memory/node, depth,
   cancellation, and progress limits.
3. `Arbitrary3x3`: deferred. A future two-phase backend needs normalized cubie
   coordinates, pruning-table lifecycle, and an explicit licensing decision.
4. `NxNReduction`: staged center solving, edge pairing, parity correction, and
   reduced 3x3 hand-off. P4L provides contracts/checkpoints before algorithms.

## NxN reduction model

The intended state machine is `Validate -> NormalizeFrame -> SolveCenters ->
PairWings -> CorrectReductionParity -> SolveReduced3x3 -> Verify`. Each phase
must expose progress, invariant checks, cancellation, bounded resource use, and
a serializable checkpoint. Even and odd sizes retain distinct center/orientation
rules. A phase may return `Unsupported` or `Incomplete`; it must not manufacture
moves or silently skip parity.

## Resource and safety contract

- Requests carry wall-clock, memory/node, depth/move, and cancellation limits.
- Progress is monotonic and names the active phase.
- Cancellation and timeout are normal typed outcomes, not crashes.
- Checkpoints include input hash, size, backend/version, phase, and bounded
  backend state; mismatched checkpoints are rejected.
- External processes, if added, run with a watchdog, no shell expansion, bounded
  output, and no network requirement by default.
- Returned moves are untrusted until parsed and replay-verified.

## Honest 11x11 status

The current 11x11 path supports physical input, inventory validation, rendering,
atomic save/load, and trusted-history reversal for states created in the active
engine session. It does not solve an arbitrary imported 11x11 state. The first
reduction milestone will therefore report framework/guidance status rather than
claiming a completed solve.
