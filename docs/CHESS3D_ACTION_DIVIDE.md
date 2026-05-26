# Chess3D Action Divide

Action divide is the explainable companion to action perft.

## ABI

- `Chess3D_DivideActionsJson(handle, depth, buffer, capacity)`

The result is JSON:

```json
{
  "format": "chess3d-action-divide",
  "version": "0.1",
  "rulesetId": "...",
  "depth": 1,
  "actions": [
    { "index": 1, "actionKind": 1, "notation": "S1 (0,0,0)->(0,0,1)", "nodes": 1 }
  ],
  "total": 1
}
```

## Purpose

Divide makes move-generation bugs reproducible: it lists first-ply actions and their subtree counts. For Rubik and Hodge, this means the diagnostic can show layer turns and projection composite moves alongside ordinary moves.

## Limits

Depth is intentionally capped at 3 for v0.1. Larger exhaustive search belongs to later AI/search tooling.
