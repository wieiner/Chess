# Chess3D Action Perft

P2O adds profile-aware action perft diagnostics. This is a correctness tool, not AI.

## ABI

- `Chess3D_PerftActions(handle, depth)`
- `Chess3D_GetLastPerftError(handle, buffer, capacity)`

Depth 0 returns 1. Depth 1 counts legal actions from the current state. Depth 2/3 recursively applies actions to copied game state and is intended for smoke diagnostics, not exhaustive deep search.

## Counted Actions

- Classic/Single-Side: normal moves and captures.
- Asgard: moves/captures plus reserve restore candidates when reserve is available.
- Rubik: Asgard-style actions plus legal layer turns.
- Hodge: projected composite moves for the active macro-player.

## Mutation Guarantee

Perft runs on copied state. It must not mutate board, CoreCell stacks, reserve counts, action history, replay cursor, or state hash.

## Limitations

Classic checkmate legality remains draft, so perft currently reflects generated legal-action contracts rather than fully king-safe orthodox 3D chess.
