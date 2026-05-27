# Chess3D P3A Regression Fixtures

P3A adds executable headless fixtures under `assets/rules/scenarios/chess3d/regression`.

## Fixtures

- `classic_self_check_illegal_v0_1.json`: exposing the own king to a rook line is rejected without mutation.
- `classic_king_cannot_move_into_check_v0_1.json`: a king cannot step onto an attacked cell.
- `classic_capture_checker_v0_1.json`: a checking rook can be captured when the capture resolves check.
- `classic_block_sliding_check_v0_1.json`: a sliding rook check can be blocked by a legal piece move.
- `classic_checkmate_micro_v0_1.json`: side in check with zero legal actions reports checkmate.
- `classic_stalemate_micro_v0_1.json`: side not in check with zero legal actions reports stalemate.
- `single_side_king_safety_smoke_v0_1.json`: Single-Side uses the same king-safety kernel when a king is present.
- `non_classic_outcome_isolation_v0_1.json`: Asgard, Rubik, and Hodge do not accidentally become Classic checkmate games.

The contract runner executes these files through the same headless playthrough path as earlier P2N/P2O scenarios.
