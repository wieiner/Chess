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
