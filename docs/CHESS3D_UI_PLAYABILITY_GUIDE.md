# Chess3D UI Playability Guide

`Chess3DApp` is now a control center for the five Chess3D RuleProfiles.

## Profile Selection

Use the Rule Profile selector to load Classic, Single-Side, Asgard, Rubik, or Hodge. The capability summary shows goal, capture, occupancy, fusion, layer, victory, and projection profile types.

## Legal Actions

Select a cell to populate the Legal actions list. Targets are highlighted in the 2D slice/full 3D preview when move hints are enabled. P2M makes target clicks preview-aware: the clicked target must match an exact preview entry before the UI dispatches a move. Invalid moves leave the board unchanged and show a reason in the Common panel and visual diagnostics.

## Mode Panels

- Classic/Single-Side: common status, selected cell, legal action list, action log.
- Asgard: core stack count, fusion kind, contested state, anchors, reserve, and restore controls.
- Rubik: layer axis/layer/quarter controls and `CanRotateLayer` status.
- Hodge: macro-player groups, mirror preview, and all-or-nothing projected move apply.

Panels whose capabilities are disabled by the current RuleProfile are collapsed so Classic stays visually clean and first-class.

## Visual Diagnostics

The visual diagnostics section reports the active model set, OBJ/fallback counts, material fallback/texture status, and last click/action rejection reason. This is meant for local QA when OBJ/MTL assets or hit-tests behave unexpectedly.

## Deferred

P2M does not add animated layer turns, replay import/export, online serialization, or full 3D check/mate UI.
## Save / Replay

The Chess3D control center includes a `Save / Replay` panel:

- save/load `.ch3dsave`;
- export/import `.ch3dreplay`;
- replay step/all;
- reset replay cursor;
- inspect state hash and last replay error.

Use savegames for bug reports that depend on constructed board/stack/reserve state. Use replay files for action-sequence reproduction.

## P2O Rule Status

The control center now surfaces more rule-state information directly in status text:

- active game phase and outcome;
- current side and macro-player where relevant;
- allowed action kinds for the selected profile;
- mode rule summary, including whether king safety is runtime, not applicable, or deferred;
- selected side legal-action count;
- last legality/invalid-action reason.

Game-over profiles should block normal play actions until reset/load/replay. Classic remains a first-class non-Asgard mode; Asgard, Rubik, and Hodge panels are profile-gated rather than globally active.

## P3A Check / Mate UI

Classic and Single-Side now report runtime check truth where a king exists. The status area shows check, checkmate, stalemate, current side legal-action count, and last legality reason. A current-side king in check is highlighted in the board UI. Asgard, Rubik, and Hodge do not show Classic checkmate as their victory condition unless a future profile explicitly opts in.

## P3B Visual Feedback

The 3D viewport now adds mode-aware overlays:

- Asgard/Rubik CoreCube cells, stack bars, fusion/contested markers, and anchors.
- Rubik layer pre-highlight before the engine commits a quarter turn.
- Hodge primary/mirror dotted arrows for projection preview and blocked mirrors.
- Short source-to-target flashes for moves and replay steps.

These hints are rebuilt from engine state after actions; they are not separate gameplay state.
