# Chess3D Fusion And Anchors

Anchors and fusion are separate layers.

## Anchor

An anchor asks:

```text
Does this target slot contain a matching side/type entry?
```

P2E made this stack-aware.

## Fusion

Fusion asks:

```text
What kind of multi-entry core state exists in this cell?
```

P2F answers with descriptors such as `friendlyPair`, `friendlyStack`, `royalPair`, and `contested`.

## Interaction

If a target slot has a matching entry and the same cell has friendly fusion for that side, P2F marks the fusion descriptor with anchored-fusion and implosion-seed flags.

## Victory

Default Asgard/Meru v0.1 victory remains compatible: `allPiecesAnchored` still wins through anchor count. Fusion progress is visible, but it does not suddenly replace victory rules unless a future profile explicitly requires that.
