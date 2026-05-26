# Chess3D Replay Format

File extension: `.ch3dreplay`.

P2N defines `chess3d-replay` version `0.1` as a deterministic action stream. It is deliberately simpler than PGN.

Required top-level fields:

- `format`: `chess3d-replay`
- `version`: `0.1`
- `initialRulesetId`
- `initialRulesJson`
- `actions`

Optional:

- `initialSaveJson`: an embedded `.ch3dsave` snapshot used when a replay starts from a constructed debug/scenario state rather than pure RuleProfile reset.
- `finalHash`: future diagnostic check.

Supported action kinds:

- `1`: move
- `2`: layer turn
- `3`: reserve restore
- `5`: Hodge projection composite move

Each action stores the same stable fields as `ActionRecord`: side, piece code/type, coordinates, layer axis/layer/quarter, reserve data, result code, flags, and notation.

Replay import is transactional at load time. Replay execution is sequential through a cursor. Failed replay actions restore the pre-action state and report `lastReplayError`.
