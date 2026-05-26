# Chess3D Playthrough Format

P2N upgrades scenario playthrough files under:

`assets/rules/scenarios/chess3d`

Runnable playthrough files use:

- `format`: `chess3d-playthrough`
- `version`: `0.1`
- `profileFile`
- `rulesetId`
- `steps`

Supported step types:

- `loadProfile`
- `clearBoard`
- `setPiece`
- `clearCell`
- `pushCore`
- `move`
- `projectedMove`
- `rotateLayer`
- `reserveRestore`
- `assertPiece`
- `assertStackCount`
- `assertReserveCount`
- `assertActionCount`
- `assertGameOver`
- `assertPreviewCountAtLeast`
- `assertLastInvalidReasonContains`

Unsupported step types fail the headless contract test with file and step context. They do not silently pass.

The playthrough runner is currently embedded in `Chess3DEngineContractTests`; a standalone tool can be split out later if CI needs richer reports.
