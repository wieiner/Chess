# Chess3D P3A Legal Perft

P3A tightens action perft for Classic and Single-Side.

## Scope

`Chess3D_PerftActions` and `Chess3D_DivideActionsJson` remain profile-aware action diagnostics, not a full AI/search feature.

For Classic/Single-Side, root and child actions are counted after king-safety filtering. For Asgard/Rubik/Hodge, the existing profile-action diagnostics continue to include their special actions where enabled.

## Guarantees

- depth 0 returns 1;
- depth 1 is based on legal actions;
- small depth 2 smoke coverage remains CI-safe;
- calls are non-mutating and verified through state-hash checks;
- divide output remains parseable JSON.

Deeper perft suites and AI/search integration remain future P3B work.
