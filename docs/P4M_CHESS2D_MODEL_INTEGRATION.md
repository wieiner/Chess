# P4M Chess2D Model Integration

Phase 38 connects the ChessApp 3D board to the shared model asset catalog without
changing chess rules, SAN, PGN, UCI, selection, hit testing, or camera controls.

## Runtime selection

`ModelAssetCatalog` discovers strict `asset-manifest-v2.json` packages and adapts
the existing `piece_sets.json` v1 catalog. A v2 package wins when the same
`setId` is present in both formats. ChessApp stores the semantic `setId`, not a
machine-local directory or display label, in `.chesssession.json`.

The role contract consists of six white pieces, six black pieces, and light and
dark board tiles. Missing roles fall back independently; an incomplete package
does not make the board unusable.

## Resolution order

1. A validated GLB role is parsed through `GlbRuntimeModelLoader`.
2. A legacy or v2 OBJ role is loaded through the existing OBJ/MTL path.
3. A missing, corrupt, or unsupported role uses the existing procedural model.

GLB file parsing is asynchronous. Selecting another set cancels the stale load.
Only conversion of the immutable runtime asset to frozen WPF objects and scene
assignment occur on the dispatcher thread.

The 3D status line reports the selected set, loaded/fallback counts, catalog
format, GLB load failures, and OBJ material diagnostics. Existing model hit
targets wrap the final `Model3D`, so clicks still resolve to the logical square.

## Session fallback

Session presentation stores `default-obj`, another semantic v2 set ID, or
`procedural`. If that set is no longer installed, ChessApp selects the first
available catalog set, or procedural when the catalog is empty, and records the
fallback reason in diagnostics. Loading the session does not fail because an
optional visual package was removed.

## Verification

The contracts cover:

- complete synthetic GLB role set discovery;
- the complete repository OBJ v1 set;
- incomplete role reporting;
- semantic set selection and deleted-set fallback;
- bounded GLB corruption, accessor, texture, extension, and limit behavior;
- WPF frozen model/cache/fallback behavior;
- the existing session serializer roundtrip;
- an x64 Release build of `ChessApp` with Visual Studio MSBuild.

No user model was added in this phase. The repository continues to use the
legacy OBJ set until an independently licensed and validated v2 set is approved.
