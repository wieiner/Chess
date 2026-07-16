# Physical Rubik Face Editor

Rubik Studio now opens a separate `Physical Editor` for U/R/F/D/L/B matrices.
Painting, drag painting, fill, clockwise face rotation, clear, undo/redo, and
copy/paste affect only `RubikFaceEditorDraft`; the native cube is untouched.

The editor reports each color count against `N*N`, empty cells, selected color,
and orientation guidance. Odd N may show a six-center suggestion for explicit
confirmation. Even N explicitly states that there is no single fixed center and
keeps the user-selected U/R/F/D/L/B orientation unchanged.

Incomplete work can be atomically saved as `.rubikdraft.json`, a separate
`rubik.editor-draft` format that allows color `0` for empty cells. It is not a
portable solved-state claim and cannot be loaded through `.rubik.json`.

`Apply to Cube` is enabled only when there are no empties and every color occurs
exactly `N*N` times. Apply creates a portable document and validated load plan,
populates a candidate native handle, and swaps the live handle only after native
acceptance. Imported cubie decomposition/history remain explicitly untrusted.
