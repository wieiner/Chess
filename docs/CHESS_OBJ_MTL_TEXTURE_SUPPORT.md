# Chess OBJ / MTL / Texture Support

P2M keeps WPF `Media3D` and does not add a heavyweight renderer.

## Supported

- OBJ vertices: `v`
- OBJ texture coordinates: `vt`
- OBJ polygon faces: `f`
- MTL library discovery through `mtllib`
- MTL fallback by same OBJ stem, for example `white_pawn.obj` -> `white_pawn.mtl`
- `map_Kd` diffuse texture when the texture exists locally
- `Ks` and `Ns` as best-effort specular hints

## Fallbacks

If MTL is missing, `map_Kd` is a placeholder, or an external texture path is unavailable, the runtime uses the visual fallback palette:

- white pieces: warm ivory
- black pieces: medium slate/charcoal, never pure black
- other Chess3D sides: distinct readable colors

## Deferred

WPF `Media3D` does not directly consume normal, roughness, metallic, or full PBR texture maps. These are ignored safely and documented in `piece_sets.json`.
