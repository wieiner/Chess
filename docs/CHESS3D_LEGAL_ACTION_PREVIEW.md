# Chess3D Legal Action Preview

P2L adds an append-only preview ABI for UI highlighting and side-panel action lists.

## ABI

- `Chess3D_BuildLegalActionPreviewForCell(handle, x, y, z, side)`
- `Chess3D_GetLegalActionPreviewCount(handle)`
- `Chess3D_GetLegalActionPreviewEntry(handle, previewIndex, entry)`
- `Chess3D_GetPreviewEntryReason(handle, previewIndex, buffer, capacity)`
- `Chess3D_GetLastInvalidActionReason(handle, buffer, capacity)`
- `Chess3D_ClearSelectionPreview(handle)`

Preview indexes are zero-based. Preview does not mutate board cells, CoreCell stacks, reserve counts, fusion descriptors, anchors, victory, or action history.

## Entry Kinds

- `Move`
- `Capture`
- `ReserveRestore`
- `LayerTurn`
- `ProjectionComposite`

Flags mark capture, knockback, core entry/exit, core-to-core movement, anchor candidates, fusion candidates, layer turns, projection composites, and simple would-end-game hints.

## UI Use

`Chess3DWindow` builds a preview when a cell is selected. The side panel lists actions with reason text, and slice/full-board markers use preview target cells for highlighting.
