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
