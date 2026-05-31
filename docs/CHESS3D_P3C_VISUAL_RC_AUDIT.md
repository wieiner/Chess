# Chess3D P3C Visual RC Audit

P3C starts from `bbc5c3f`: five real Chess3D RuleProfiles, engine-backed king safety for Classic/Single-Side, save/load/replay, legal preview, profile-gated Asgard/Rubik/Hodge actions, and the P3B overlay layer.

## Current Visual Surface

- `Chess3DWindow.xaml` hosts the profile selector, common play panel, mode panels, action log, save/replay panel, scenario list, 2D slice grid, and `Viewport3D`.
- `Chess3DWindow.xaml.cs` builds the board, piece models, legal target markers, CoreCube overlays, stack badges, fusion rings, Hodge arrows, Rubik layer highlights, and replay/action flashes.
- `Chess3DVisualDescriptors.cs` contains cell/action descriptors and the P3B overlay palette.
- `ObjModelLibrary.cs` loads OBJ meshes, best-effort MTL diffuse textures, and readable fallback materials.

## Existing Overlays

- Selected source cell.
- Legal and capture target markers.
- Current-side king-in-check marker.
- CoreCube wash for Asgard/Rubik profiles.
- Anchor marker.
- Stack badge bars for multi-entry CoreCell stacks.
- Fusion/contested/royal/implosion rings.
- Rubik selected-layer wash.
- Hodge primary/mirror/blocked dotted paths.
- Short action/replay flash paths.

## Existing Animations

P3B added short UI-only flashes and Rubik layer pre-highlight. They do not move engine state directly; the native engine remains the source of truth.

## Mode-Aware UI

- Classic: common panel, legal actions, check/outcome text, no Asgard/Rubik/Hodge panels.
- Single-Side: common training panel, legal preview.
- Asgard: core/stack/fusion/reserve/anchor panel.
- Rubik: layer turn panel and layer overlay.
- Hodge: projection panel and mirror arrows.

## Engine State Available Through ABI

The UI reads ruleset/profile summaries, legal preview, invalid reasons, turn summary, action history, save/replay state, state hash, stack counts/entries, fusion descriptors, anchor state, reserve/knockback state, layer-turn info, projection transforms/errors, and Classic check status.

## Remaining UX Risks

- WPF frame-by-frame animation is not covered by headless CI.
- Overlays must stay non-authoritative and should not become logical board cells.
- Dense all-layer views can still be visually busy.
- Manual QA is required for contrast, camera comfort, and whether arrows/rings feel readable in real play.

## Safe P3C Work

P3C can safely add a UI-only visual state snapshot, camera/readability controls, clearer visual diagnostics, animation locking hygiene, and manual QA docs. It should not change native move legality, save/replay formats, or RuleProfile semantics.
