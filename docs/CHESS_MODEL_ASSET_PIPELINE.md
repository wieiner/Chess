# Chess Model Asset Pipeline

P2M canonical model assets live under:

```text
assets/models/chess/pieces
```

The current production set is:

```text
assets/models/chess/pieces/default
```

The catalog is:

```text
assets/models/chess/pieces/piece_sets.json
```

## Runtime Layout

Both Chess2D and Chess3D copy the canonical set to:

```text
Assets/Models
```

inside development output and portable `ProductionOutput`.

Expected runtime examples:

- `Assets/Models/piece_sets.json`
- `Assets/Models/default/Pieces/white_pawn.obj`
- `Assets/Models/default/Pieces/black_pawn.mtl`
- `Assets/Models/default/Board/light_tile.obj`

## Policy

- ZIP archives are source/import material, not required runtime content.
- Only production OBJ/MTL/texture assets should be tracked.
- Missing OBJ files fall back to procedural primitives.
- Missing MTL or texture files fall back to readable procedural materials.
- `rude-resource/` remains ignored and is not part of this pipeline.

P3B overlays are procedural WPF geometry and do not require new OBJ/MTL files. They layer on top of the canonical model catalog.

P3C keeps that asset boundary. The visual diagnostics panel now surfaces active model set, OBJ/fallback model counts, overlay count, animation state, visual state, option state, and the last material/model diagnostic text. P3C does not add new model formats or automatically unpack ZIP archives.

## Generated Piece Sets

P4C phase 10 adds a generated-asset contract without adding heavy generated meshes:

```text
assets/models/chess/pieces/generated/piece_set.generated.example.json
```

The example manifest is disabled and descriptor-only. It defines the rules for future generated piece sets:

- OBJ/MTL remains the supported runtime format today;
- glTF/GLB is the preferred future interchange/runtime format once a loader is added;
- every enabled set needs source/license metadata, size review, fallback materials, scale/origin data, and path validation;
- no absolute local paths, ZIP dumps, cache files, or heavy binaries should be committed without audit.

The detailed policy lives in `docs/CHESS3D_GENERATED_PIECE_ASSET_PIPELINE.md`.
