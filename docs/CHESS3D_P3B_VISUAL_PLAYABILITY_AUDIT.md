# Chess3D P3B Visual Playability Audit

P3B starts after the separate P3A king-safety commit. It does not add game modes or change the five RuleProfiles.

## Current 3D Models

`Chess3DWindow` builds a WPF `Viewport3D` from tile meshes, OBJ piece meshes when available, and procedural cube fallback meshes. `ObjModelLibrary` resolves OBJ/MTL files from the canonical `Assets/Models` catalog and falls back to readable WPF materials.

## Materials And Lighting

P2M already moved black pieces away from pure black and added ambient/key/rim lighting. P3B keeps that pipeline and adds a visual descriptor/theme layer so overlays use named colors instead of one-off magic values.

## Selection And Preview

Click-to-move already uses legal preview as the source of truth. P3B keeps this contract and adds visible action hints:

- selected cell marker;
- legal/capture markers;
- current-side king-in-check marker;
- action flash after move/replay;
- input lock during layer-turn animation.

## Missing Before P3B

- Core stacks were mostly text-only.
- Fusion/contested/anchor state was mostly text-only.
- Rubik layer turns executed instantly.
- Hodge mirror moves were text preview only.
- Replay step refreshed state without visual action feedback.

## Safe P3B Changes

P3B is UI-layer only. Native rules, save/replay formats, action history, profile JSON, and ABI semantics stay unchanged.
