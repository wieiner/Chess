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

For Classic/Single-Side after P3A, divide roots are filtered through the king-safety legal move layer. Self-check and king-into-check roots should not appear in the JSON.

P3D uses the same root-action surface for AI candidates. Divide stays an explainability diagnostic; AI search adds scoring and best-action selection.

P3D.1 keeps divide as a legal-action diagnostic. The strengthened AI search adds ordering, alpha-beta, iterative deepening, and bounded quiescence-lite around the same root-action surface; divide itself does not mutate state and does not become a strength benchmark.

## Limits

Depth is intentionally capped at 3 for v0.1. Larger exhaustive search belongs to later AI/search tooling.
