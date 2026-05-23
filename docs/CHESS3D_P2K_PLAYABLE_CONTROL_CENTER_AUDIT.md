# Chess3D P2K Playable Control Center Audit

P2K starts from the P2J runtime: Chess3D already has RuleProfile loading, action history, reserve restore, Rubik layer turns, CoreCell stacks, fusion descriptors, knockback/reserve captures, and Hodge projected composite moves.

## Profiles

The runtime profile assets are copied to `Assets/Rules3D/Profiles`:

- `classic_six_side_3d_v0_1.json`
- `single_side_3d_v0_1.json`
- `asgard_convergence_3d_v0_1.json`
- `rubik_convergence_3d_v0_1.json`
- `hodge_projection_duel_3d_v0_1.json`

These profiles are separate modes. Asgard is not the default meaning of Chess3D.

## UI Before P2K

`Chess3DWindow` already displayed a long text summary and allowed raw JSON loading, manual setup, normal moves, AI moves, legacy Rubik rotate controls, full/slice view selection, network controls, and 3D navigation.

The engine capabilities were present but mostly hidden behind text:

- profile capability flags;
- CoreCell stack selected-cell state;
- fusion kind and contested state;
- reserve/knockback state;
- layer-turn availability/result;
- Hodge projection availability/errors;
- action history notation.

## ABI Available To UI

The UI can safely query:

- current ruleset and profile types;
- stack/fusion/reserve/layer/projection capabilities;
- selected cell stack/projected/fusion state;
- anchor/victory state;
- reserve restore and last knockback information;
- layer-turn validation/result state;
- Hodge projection group/transform/action state;
- action history and notation.

P2K needed only C# wrapper exposure for a few existing exports: display name, core physics profile, victory profile, last profile error, and fusion recompute.

## Safe P2K Actions

Safe UI actions are:

- load a selected RuleProfile JSON;
- recompute fusion;
- auto-restore reserve for active side/type;
- rotate a layer when the active profile permits it;
- preview/apply Hodge projected moves through `Chess3D_TryMakeProjectedMove`;
- view/copy/save action history;
- list scenario descriptors.

## Future UI Actions

The following remain later work:

- inventory drag/drop restore;
- stack-entry editor;
- animated Rubik turns;
- fusion/implosion visual effects;
- replay import/export;
- online serialization controls;
- AI/search controls for Hodge/Rubik actions.

