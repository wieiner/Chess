# Chess3D Hodge Projection ABI

P2J adds append-only native exports. Existing ABI functions are unchanged.

## Profile / Group Queries

```text
Chess3D_IsProjectionModeEnabled(handle)
Chess3D_GetProjectionMacroPlayerCount(handle)
Chess3D_GetProjectionCountForMacroPlayer(handle, macroPlayer)
Chess3D_GetProjectionSide(handle, macroPlayer, projectionIndex)
Chess3D_GetMacroPlayerForSide(handle, side)
Chess3D_GetProjectionProfileSummary(handle, buffer, capacity)
Chess3D_GetLastProjectionError(handle, buffer, capacity)
```

## Transform / Action

```text
Chess3D_TransformMoveBetweenSides(handle, sourceSide, targetSide, from, to, outFrom, outTo)
Chess3D_TryMakeProjectedMove(handle, primarySide, from, to, promotionType, playedMove)
```

## Action History

Successful projected moves append action kind:

```text
ActionProjectionCompositeMove = 5
```

and set:

```text
ActionFlagWasProjection = 512
```

String APIs are buffer/capacity based and remain null-termination safe.
