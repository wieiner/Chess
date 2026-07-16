# Rubik Solver Contracts

`RubikState` now owns a platform-neutral solver boundary. A request contains a
validated portable state, time/memory/depth bounds, cancellation, optional
checkpoint, optional trusted history, and progress reporting. A result contains
moves, phases, elapsed time, input/final hashes, independent verification state,
and a typed failure.

`ReverseHistorySolver` is the first implementation. Its capabilities explicitly
say `SupportsArbitraryState=false` and `RequiresTrustedHistory=true`. It reverses
and inverts only trusted engine history, rejects imported states without that
history, and returns verification status `NotRun`. Phase 23 adds the independent
replay authority that may promote a result to `Verified`.

The contracts have no WPF or native dependency. Future bounded 2x2, two-phase
3x3, and NxN reduction backends can implement `IRubikSolver` without changing
the portable state file or native ABI.
