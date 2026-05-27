# Chess3D Stack And Fusion Visuals

P3B adds lightweight Forbidden Core overlays in `Chess3DWindow`.

## Stack Display

When a visible core cell has more than one stack entry, the top/projected piece remains the main model and small gold stack bars appear above the cell. The bars are visual hints only and do not affect hit-testing or stack data.

## Fusion Display

Fusion descriptors are shown as cell overlays:

- friendly pair/stack: soft green ring/plate;
- royal pair: stronger gold halo;
- contested/mixed: red contested marker;
- implosion seed/ready: purple progress marker.

## Anchors

Anchored target cells receive a small gold anchor marker. Anchor and fusion overlays are intentionally non-destructive; stack entries remain the source of truth.

## Limitations

P3B does not animate individual stack entries, show full piece-by-piece stack inventories in the viewport, or implement destructive implosion effects.
