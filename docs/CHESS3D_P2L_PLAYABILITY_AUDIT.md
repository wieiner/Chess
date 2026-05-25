# Chess3D P2L Playability Audit

P2L starts from the P2K playable control center on `main`.

## Real Rule Profiles

There are exactly five real Chess3D RuleProfile JSON files under `assets/rules/profiles`, excluding `chess3d_rule_profile.schema.json`:

- `classic_six_side_3d_v0_1.json`
- `single_side_3d_v0_1.json`
- `asgard_convergence_3d_v0_1.json`
- `rubik_convergence_3d_v0_1.json`
- `hodge_projection_duel_3d_v0_1.json`

No sixth Chess3D RuleProfile exists in the repository at P2L. Scenario files under `assets/rules/scenarios/chess3d` are smoke/playthrough descriptors, not additional modes. Chess2D, RubikApp, and documentation-only artifacts are also not Chess3D RuleProfiles.

## Current Playability

- Classic Six-Side is playable-draft: normal 3D piece movement and classic captures work, but full 3D king safety/check/mate/stalemate remain draft.
- Single-Side is training/playable: it exposes the one-side movement core and is useful for UI and piece-rule debugging.
- Asgard Convergence is experimental-playable: core stacks, fusion descriptors, anchors, knockback/reserve, and reserve restore exist, while contested anchor policy and destructive implosion remain deferred.
- Rubik Convergence is experimental-playable: Asgard-style state plus profile-gated Rubik layer turns over projected board and core stacks.
- Hodge Projection Duel is experimental-playable: two macro-players, three projections each, all-or-nothing projected composite moves, and HPD notation.

## UI Visibility

`Chess3DWindow` already exposes a RuleProfile selector from `Assets/Rules3D/Profiles`, action log, and mode-aware panels. P2L adds a runtime legal action preview list, turn summary, invalid-action reason text, and collapses special panels when the active profile does not enable that capability.

## Existing Gaps

- Full 3D check/mate is still not hardened for Classic Six-Side.
- Visual highlights are simple target markers, not animated path overlays.
- Scenario playthroughs are headless smoke contracts, not full replay/import files.
- UI remains a control center rather than a polished final game shell.

## Safe P2L Changes

- Append-only preview/turn ABI.
- UI-side legal action lists and clearer invalid reasons.
- Scenario playthrough descriptors for all five profiles.
- Documentation that separates Classic, Single-Side, Asgard, Rubik, and Hodge without treating Asgard as the whole game.
