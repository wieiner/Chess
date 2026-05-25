# Chess3D UI Playability Guide

`Chess3DApp` is now a control center for the five Chess3D RuleProfiles.

## Profile Selection

Use the Rule Profile selector to load Classic, Single-Side, Asgard, Rubik, or Hodge. The capability summary shows goal, capture, occupancy, fusion, layer, victory, and projection profile types.

## Legal Actions

Select a cell to populate the Legal actions list. Targets are highlighted in the 2D slice/full 3D preview when move hints are enabled. Invalid moves leave the board unchanged and show a reason in the Common panel.

## Mode Panels

- Classic/Single-Side: common status, selected cell, legal action list, action log.
- Asgard: core stack count, fusion kind, contested state, anchors, reserve, and restore controls.
- Rubik: layer axis/layer/quarter controls and `CanRotateLayer` status.
- Hodge: macro-player groups, mirror preview, and all-or-nothing projected move apply.

Panels whose capabilities are disabled by the current RuleProfile are collapsed so Classic stays visually clean and first-class.

## Deferred

P2L does not add animated layer turns, replay import/export, online serialization, or full 3D check/mate UI.
