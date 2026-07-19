# P4M Unified Model Asset Repository Audit

## Inventory

The current runtime model root is `assets/models/chess/pieces`. It contains one enabled v1 set (`default-obj`), one disabled generated descriptor, 14 OBJ files, 14 MTL files, and no colocated texture images. OBJ payload is 25,581,882 bytes; MTL payload is 3,792 bytes. Twelve piece meshes dominate the set: white files are about 1.66 MiB each and black files about 2.60 MiB each. The two board-tile meshes are below 1 KiB.

No tracked GLB, glTF, FBX, Blend, model ZIP, normal, roughness, or diffuse texture exists under the runtime root. The PNG/BMP files under `src/ChessApp/Assets/Figures` are 2D piece sprites, not 3D material textures.

## Catalog and loaders

- `piece_sets.json` v1 catalogs six white/black piece roles plus light/dark tiles for Chess2D and Chess3D.
- `ObjModelLibrary` is shared as source/link between ChessApp and Chess3DApp. It parses OBJ positions/normals/UVs and best-effort MTL hints, then uses readable procedural materials when texture resolution fails.
- Set discovery is directory-based rather than manifest-authoritative. It can expose a directory whose manifest is disabled or incomplete.
- ChessApp and Chess3DApp copy the complete piece root to `Assets/Models`; production packaging copies those application outputs.
- RubikApp uses its procedural facelet/cubie renderer and has no model catalog integration.

## Materials and provenance findings

The enabled set has no complete license file or author/source provenance record. Its README says the files were migrated from a previous local application path. This is not sufficient evidence for redistribution approval.

Several white MTL files contain an absolute `E:/...` source texture path; black MTL files use `map_Kd .`. Neither resolves to a tracked texture. Runtime currently falls back safely, but absolute private source paths violate the planned package policy and must be sanitized before the v2 set is declared QA-enabled.

The generated example is correctly disabled and declares `replace-before-use`; it references no tracked mesh and is not a runtime set.

## Packaging and fallback

`scripts/verify.ps1` asserts the v1 catalog, representative OBJ/MTL files, generated descriptor, development outputs, and both Chess2D/Chess3D ProductionOutput copies. Missing models/materials fall back to procedural WPF geometry and an ivory/medium-slate palette. The fallback is the only fully provenance-independent visual path today.

## Risks and decisions

1. Keep v1 compatibility while introducing a closed v2 manifest and adapter.
2. Treat current OBJ assets as legacy runtime inputs pending explicit license/provenance review.
3. Do not infer a license from Blender exporter comments.
4. Do not add Git LFS or new heavy assets before size/churn policy approval.
5. Separate authored source formats from optimized runtime formats.
6. Sanitize absolute texture paths and validate all relative paths/hashes.
7. Prefer GLB at runtime only after a bounded parser boundary and WPF conversion tests exist; retain OBJ and procedural fallback.
