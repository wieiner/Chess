# Chess3D State Hash

P2N adds `Chess3D_GetStateHash`.

The hash is a deterministic diagnostic fingerprint, not a cryptographic security primitive.

It includes:

- `rulesetId`;
- current side and derived macro-player;
- projected 512-cell board;
- CoreCell stack entries;
- reserve counts;
- `gameOver` / `winnerSide`;
- action count and action notation.

It is used by contract tests to prove:

- save/load roundtrips preserve state;
- replay reaches the same final state as the exported action stream;
- failed load/replay attempts do not mutate state.

Future network sync may need a separate protocol hash with versioned canonicalization.
