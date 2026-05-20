# Chess3D P2F Fusion Audit

P2F starts from the P2E CoreCell stack runtime.

## 1. Current CoreCell stack

`Chess3DEngine.dll` keeps the legacy `Position::board` as 512 integer cells. For stack-enabled profiles, Forbidden Core cells also have `Game::coreStacks[index]`, a vector of stack entries. The projected board cell is synchronized to the top stack entry.

## 2. Stack entry flags

`CoreStackEntry` currently stores:

- `side`;
- `pieceType`;
- `pieceCode`;
- `flags`.

The `flags` field is reserved for later per-entry state. P2F does not need to mutate entries to represent fusion.

## 3. Stack-aware anchors

P2E anchor recomputation scans target slots and, when stacks are enabled, searches every stack entry for matching `side/type`. A target slot counts once even if multiple matching entries exist.

## 4. CenterAssembly victory

The existing centerAssembly victory path still uses `anchorCounts[side] >= requiredAnchorCount`. P2F keeps that compatible behavior and adds fusion/implosion progress as a parallel descriptor layer.

## 5. Stack without fusion before P2F

Before P2F, cells could hold several entries, but there was no runtime answer for:

- friendly pair vs friendly stack;
- royal king/queen pair;
- enemy contested co-occupancy;
- side-level fusion counts;
- implosion/completion progress.

## 6. Safe P2F additions

Safe changes are append-only:

- add `CoreFusionState` overlay beside `coreStacks`;
- recompute descriptors from stack entries;
- expose new ABI getters;
- keep stack entries as source of truth;
- keep old projected board ABI unchanged.

## 7. Not for P2F

P2F must not implement knockback/reserve, Rubik stack rotations, online serialization, GPU stack snapshots, destructive merges, unique piece ids, or final Volume-Surface 216 mechanics.
