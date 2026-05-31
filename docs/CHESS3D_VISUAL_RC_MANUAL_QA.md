# Chess3D Visual RC Manual QA

Headless CI verifies engine behavior, packaging, and scenario/regression playthroughs. It does not prove WPF frame readability. Use this checklist before calling a visual build release-ready.

## Classic

1. Load Classic Six-Side.
2. Select a current-side piece.
3. Confirm legal and capture targets are distinct.
4. Attempt an illegal self-check move.
5. Confirm the reason is visible and state does not mutate.
6. Load/check a checkmate or stalemate fixture/replay and confirm outcome text is visible.

## Single-Side

1. Load Single-Side Training.
2. Confirm training/sandbox status is visible.
3. Select pieces and confirm clean legal preview.
4. Confirm king-safety smoke status is not misrepresented as full six-side competitive play.

## Asgard

1. Load Asgard.
2. Create or replay a core stack scenario.
3. Confirm CoreCube wash is visible but not overwhelming.
4. Confirm stack bars, fusion ring, contested marker, anchors, reserve and knockback status are readable.

## Rubik

1. Load Rubik Convergence.
2. Choose axis/layer/quarter turn.
3. Confirm selected layer highlight appears before commit.
4. Confirm input lock prevents double turn.
5. Run four identical quarter turns and confirm the state hash returns where expected.

## Hodge

1. Load Hodge Projection Duel.
2. Preview mirrors.
3. Confirm primary and two mirror arrows.
4. Apply projected move and confirm all pieces update together.
5. Try a blocked mirror fixture and confirm rollback plus blocked arrow styling.

## Save / Replay

1. Save a game.
2. Export replay.
3. Reset.
4. Import replay.
5. Replay step and confirm visual flash does not duplicate actions.
6. Replay all and confirm final status/hash.

## Models / Materials

1. Confirm black pieces are visible on the default background.
2. Toggle high contrast.
3. Confirm OBJ/MTL diagnostics are readable.
4. Missing textures should produce fallback diagnostics, not crashes.
