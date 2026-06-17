# Chess3D Generated Piece Asset Pipeline

Phase: P4C phase 10.

## Purpose

Generated chess pieces should enter the project through the same visible, testable pipeline as the existing OBJ/MTL set. They must not arrive as loose ZIP archives, local absolute paths, cache folders, or heavy binary surprises.

## Supported Formats

Current runtime:

- OBJ mesh files;
- MTL material files;
- optional diffuse/specular texture files referenced by MTL.

Future preferred format:

- glTF/GLB, because Khronos defines glTF as an efficient runtime 3D asset delivery format that bridges DCC tools and graphics applications, while minimizing runtime processing and transfer size.

P4C does not add a GLB loader. GLB is the recommended future import/export target for generated pieces once the WPF/runtime loader is extended.

Sources used:

- Khronos glTF overview: https://www.khronos.org/gltf/
- Khronos glTF 2.0 specification: https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html

## Folder Layout

Canonical root:

```text
assets/models/chess/pieces
```

Current production set:

```text
assets/models/chess/pieces/default
```

Generated-set staging:

```text
assets/models/chess/pieces/generated/<set-id>/
```

Descriptor example:

```text
assets/models/chess/pieces/generated/piece_set.generated.example.json
```

The example manifest is disabled and contains no real meshes. It documents the contract for future generated sets.

## Naming

Use stable lowercase names:

```text
white_pawn.obj
white_pawn.mtl
black_pawn.obj
black_pawn.mtl
```

Required piece types:

- pawn
- rook
- knight
- bishop
- queen
- king

`officer` may alias bishop in profile metadata, but a generated visual set only needs the six standard piece shapes unless a future profile requires more.

## Transform Contract

Every generated set must declare:

- scale;
- rotation;
- vertical offset;
- origin policy.

Default expectation:

- origin centered on the board square;
- piece standing on the square plane;
- scale compatible with current Chess2D and Chess3D cells.

## Material And Texture Rules

Each set must declare readable fallbacks:

- white: warm ivory/light material;
- black: slate/charcoal, not pure black.

Supported now:

- diffuse colors;
- MTL `Kd`/`Ks`/`Ns` best effort;
- MTL `map_Kd` diffuse textures when files are present.

Deferred:

- normal maps;
- roughness/metalness/PBR maps;
- skinning and animation;
- GLB-native PBR material import.

Unsupported maps must be ignored safely, not crash loading.

## Source And License Metadata

Every generated set must record:

- generator/tool/source;
- author/owner;
- license;
- whether redistribution is allowed;
- generation prompt or art brief if appropriate;
- manual edits, if any.

Unknown-license assets must not be enabled or shipped.

## Size Policy

Default guidance:

- individual mesh/texture files over 5 MB need review;
- full sets over 50 MB need size audit;
- files over 10 MB should mention whether Git LFS is required.

Do not commit:

- ZIP source archives unless explicitly approved;
- Blender temp/cache files;
- `.blend1`, autosaves, previews, thumbnails unless intentionally part of docs;
- local export logs;
- absolute local paths in manifests.

## Validation

Before enabling a generated set:

1. Manifest parses as JSON.
2. No absolute paths.
3. All declared required files exist.
4. Six required piece types are present for white and black.
5. Materials have readable fallbacks.
6. Scale/origin verified in Chess2D and Chess3D.
7. License/source metadata reviewed.
8. No heavy binaries without explicit audit.

Phase 10 adds tests for the disabled example manifest and verify checks that it is copied to Chess2D/Chess3D development and production output.

## Git Tracking Policy

Track:

- reviewed production OBJ/MTL/texture assets;
- small JSON manifests;
- docs and validation fixtures.

Do not track:

- private local resource archives;
- `rude-resource/`;
- generated cache/temp files;
- secrets or license-private source bundles;
- heavyweight generated sets without a size/license decision.

## Adding A New Generated Set

1. Create `assets/models/chess/pieces/generated/<set-id>/`.
2. Add meshes/materials/textures with stable names.
3. Add or extend a manifest descriptor.
4. Validate paths, sizes, license, and fallback materials.
5. Add tests for the new set.
6. Run `tests/run-tests.ps1 -SkipBenchmark`.
7. Run `scripts/verify.ps1`.
8. Only then commit.
