# Chess3D Playable Control Center

P2K turns `Chess3DApp` into a mode-aware control center. The app can load rule profiles from runtime assets, show active capabilities, expose mode-specific action panels, and display the action log.

## Profile Selector

Profiles are discovered from `Assets/Rules3D/Profiles`. The selector loads:

- Classic Six-Side;
- Single-Side training;
- Asgard / Meru Convergence;
- Rubik Convergence;
- Hodge Projection Duel.

Profile load failures are shown in the UI through the engine profile error string. A failed load does not intentionally destroy the previous valid profile state.

## Mode Panels

The control center uses mode-aware panels:

- Common: selected cell, active side, selected legal move count, action count, last notation.
- Asgard / Core: stack count, projected piece, fusion kind, contested state, anchors, reserve, last capture, auto restore.
- Rubik Layer Turn: axis, layer, quarter turn, capability check, last result.
- Hodge Projection: macro-player groups, primary side, from/to coordinates, mirror preview, all-or-nothing projected move apply.
- Action Log: latest actions, copy, save.

Disabled panels stay visible as capability indicators, but their actions clean-fail through the engine if the profile does not allow them.

## Scenario Smoke Pack

P2K adds JSON descriptors under `Assets/Rules3D/Scenarios`. They are not replay files; they are compact manual/testing descriptors that say which profile and capabilities to smoke-check.

## P2L Playability Closure

P2L adds legal action preview to the same control center. Selecting a cell now produces a non-mutating preview list with action kind, target coordinate, flags, and reason text. The UI uses that preview for target highlighting and invalid-action explanations.

Special panels are mode-aware: Classic and Single-Side keep Asgard/Rubik/Hodge panels collapsed, while Asgard, Rubik, and Hodge expose only the panels their active RuleProfile enables.
