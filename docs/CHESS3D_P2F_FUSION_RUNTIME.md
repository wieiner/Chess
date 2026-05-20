# Chess3D P2F Fusion Runtime

P2F adds `CoreFusionState` arrays to `Chess3DEngine.dll`.

## Runtime Flow

After stack-affecting operations, the engine recomputes:

1. projected board cache;
2. fusion descriptors;
3. stack-aware anchors;
4. implosion progress;
5. compatible centerAssembly victory.

Operations that refresh this state include reset, clear, profile load, set board, set piece, stack push, stack remove, stack clear, and successful moves.

## Profile Isolation

Fusion is enabled only when:

- the active profile enables CoreCell stacks; and
- `fusionProfile.type` is not `none`.

Classic and single-side profiles report fusion disabled and return `none` for fusion kind.

## Non-Destructive Semantics

The descriptor layer never destroys stack entries. A contested cell remains a stack with entries from multiple sides. A royal pair remains king and queen entries in the stack.

## Current Limits

No knockback/reserve, Rubik stack rotations, visual effects, online serialization, GPU stack snapshots, or Volume-Surface 216 victory are implemented in P2F.
