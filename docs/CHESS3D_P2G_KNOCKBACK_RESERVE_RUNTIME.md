# Chess3D P2G Knockback / Reserve Runtime

P2G implements runtimePartial knockback/reserve behavior for Asgard/Meru profiles.

## Runtime Model

`Game` now stores:

```text
reserveCounts[7][7]
lastCaptureWasKnockback
lastCapturedPieceCode
lastKnockbackDestination
lastKnockbackHomeX/Y/Z
```

Reserve is cleared on reset, clear, board sync, and profile reload.

## Capture Routing

`applyMove(Game&, Move)` now routes captured pieces before the destination is overwritten:

- classic profile: captured piece is removed and telemetry reports `classicRemoved`;
- Asgard/Rubik profile: captured piece goes to home or reserve;
- core destinations do not trigger knockback.

## Home Slot Policy

Home slots are derived from the existing face-centered 4x4 setup pattern. Matching means same side and piece type. Since there are no unique piece ids, any same-type home slot is valid.

## Recompute

After successful moves, the engine still recomputes:

1. projected board and stacks;
2. fusion descriptors;
3. anchors;
4. implosion progress;
5. compatible victory.

Reserve counts are not affected by fusion recompute.

## UI

Chess3D status text now exposes:

- reserve enabled;
- knockback enabled;
- current side reserve total;
- last capture destination;
- last captured piece code.
