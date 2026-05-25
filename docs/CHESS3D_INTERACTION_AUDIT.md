# Chess3D Interaction Audit

P2M audited the "selected piece shows targets, but click target does not move" issue.

## Findings

- P2L legal preview is generated through `Chess3D_BuildLegalActionPreviewForCell`.
- The 2D layer grid used selected coordinates and direct `TryMakeMove`.
- The 3D viewport hit-test mapped models to logical cells, but `HandlePickedSquare` also called direct `TryMakeMove`.
- Direct `TryMakeMove` is insufficient for mode-specific actions:
  - Hodge projected moves require `TryMakeProjectedMove`.
  - Rubik layer turns are separate actions.
  - Reserve restore is not a cell click move.
- Failed direct moves could leave the player with only a generic engine rejection and no clear preview mismatch explanation.

## P2M Fix

The UI now routes click-to-move through exact legal-preview matching:

1. Build preview for the selected source cell.
2. Find an entry whose source and target match the clicked target.
3. Dispatch by preview action kind:
   - normal move/capture -> `TryMakeMove`;
   - projection composite -> `TryMakeProjectedMove`;
   - other action kinds stay in their mode-specific panels.
4. If there is no matching preview entry, show a target-specific invalid reason and keep the board state unchanged.

This does not change engine rules; it makes UI dispatch consistent with the preview contract.
