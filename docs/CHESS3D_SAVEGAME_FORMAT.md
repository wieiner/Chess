# Chess3D Savegame Format

File extension: `.ch3dsave`.

P2N defines `chess3d-savegame` version `0.1` as a full JSON snapshot for diagnostics, bug reproduction, and local play continuity.

Required top-level fields:

- `format`: `chess3d-savegame`
- `version`: `0.1`
- `rulesetId`
- `rulesetFileName` or future relative path
- `rulesJson`: embedded RuleProfile JSON
- `board`: dimensions, currently always `8x8x8`
- `currentSide`
- `currentMacroPlayer`
- `currentTurnKind`
- `projectedBoard`: 512 piece codes
- `coreStacks`: non-empty CoreCell stacks
- `reserveCounts`
- `gameOver`
- `winnerSide`
- `actionHistory`
- `recomputeFusionOnLoad`
- `recomputeAnchorsOnLoad`

`LoadSaveGameJson` is transactional: invalid JSON, invalid pieces, invalid core-stack cells, or ruleset mismatch leave the previous game state unchanged.

Fusion, anchors, implosion progress, and victory descriptors are recomputed after load. This keeps derived overlays honest instead of trusting stale serialized descriptors.

P3A also recomputes Classic/Single-Side check, checkmate, stalemate, game-over state, and winner/no-winner from the loaded board. Older v0.1 save files remain loadable; the runtime derives the stronger outcome truth after load.

Known limitation: this is not the online/network save protocol. It is a local JSON snapshot format.
