# Chess Visual Asset Audit

P2M audited the current visual pipeline after P2L.

## Findings

- Chess2D 3D-model mode is implemented in `src/ChessApp/MainWindow.xaml.cs`.
- Chess3D 3D preview/control-center rendering is implemented in `src/ChessApp/Chess3DWindow.xaml.cs`; `src/Chess3DApp` links that WPF window and wrapper code.
- Before P2M, Chess2D had a private OBJ parser while Chess3D used `ObjModelLibrary`. Both applied WPF side colors and did not use MTL/texture metadata.
- Existing local OBJ assets were under `src/ChessApp/Assets/Models/Default`.
- No production ZIP model package was found outside ignored/generated folders. No `rude-resource/` asset was imported.
- The OBJ set includes `*.obj` and `*.mtl`; no colocated diffuse textures were found.
- White MTL files reference external absolute texture paths that are not in the repo. Black MTL files use `map_Kd .`.
- Chess3D used a near-black preview background (`#111418`) and relatively dark black-piece colors, so black models were hard to read.
- Chess3D hit-test maps WPF `Model3D` instances to logical cells through `_hitSquares`.
- Before P2M, 3D click-to-move dispatched directly to `TryMakeMove`; it did not require an exact legal-preview entry and did not dispatch Hodge projection actions.

## Safe P2M Changes

- Move local OBJ/MTL assets into a canonical repo-level asset catalog.
- Keep old procedural fallback meshes.
- Use readable fallback materials when MTL texture references are missing.
- Add MTL/texture-coordinate best-effort support without adding a new 3D engine dependency.
- Make Chess3D click dispatch preview-aware and show invalid reasons.
- Keep all five Chess3D RuleProfiles unchanged as game modes.
