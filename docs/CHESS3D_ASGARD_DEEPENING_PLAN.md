# Chess3D Asgard Deepening Plan

Phase: P4C phase 08.

This plan lists the future work needed before Asgard / Meru Convergence can move from `runtimePartial` to a fully tuned product mode. It intentionally keeps current P4C runtime behavior unchanged.

## Current Foundation

Asgard already has:

- CoreCell stacks in the Forbidden Core;
- stack-aware anchors;
- fusion descriptors;
- implosion progress descriptors;
- knockback and reserve captures;
- reserve restore to matching free home slots;
- action history, save/load/replay, state hash;
- online authority smoke;
- shallow AI/search candidates.

## Deepening Work

### Final Fusion Behavior

Decide whether friendly fusion remains only a descriptor or becomes an actionable entity. If it becomes actionable, define:

- when fusion starts;
- whether stack entries remain individually addressable;
- whether fusion changes movement, capture, anchor scoring, or victory;
- how replay/save/load represent the result.

### Destructive vs Descriptor-Only Fusion

Current runtime is descriptor-only. A destructive model must be a separate rule contract because it can remove or transform stack entries. Required decisions:

- merge target piece code or entity type;
- reversibility through replay;
- contested cell handling;
- UI and online synchronization.

### Implosion Resolution

Current implosion is progress state. A final event needs:

- trigger threshold;
- winner/outcome mapping;
- board mutation rules;
- animation and action notation;
- deterministic replay semantics.

### Victory Variants

Base Asgard uses `allPiecesAnchored`. Future variants may add:

- fusion-count requirements;
- royal-pair requirements;
- contested-anchor denial;
- hybrid centerAssembly plus check pressure.

Each variant should be explicit in JSON and isolated from Classic checkmate.

### Reserve UI and Restore Semantics

Reserve currently stores side/type counts and restores only to matching free home slots. Future work:

- inventory panel;
- restore candidate preview;
- restore into core policy;
- restore capture policy;
- maximum reserve constraints by piece type.

### Balance and Playtesting

Asgard needs playtesting data for:

- core-entry speed;
- anchor count thresholds;
- contested cell frequency;
- reserve comeback strength;
- whether side order gives first-player advantage.

### AI Scoring

Current AI/search is shallow and profile-aware. Deeper Asgard evaluation should score:

- anchor progress;
- fusion descriptors;
- contested danger;
- reserve inventory;
- core distance;
- opponent completion threats.

### Online Snapshot Fidelity

Online state must preserve:

- projected board;
- core stacks;
- fusion/anchor recomputation inputs;
- reserve counts;
- action history;
- current turn;
- game outcome.

Snapshots should remain state-hash stable after load/replay.

### Replay Fidelity

Replay needs deterministic action records for:

- entering core stacks;
- knockback to home/reserve;
- reserve restore;
- future fusion/implosion actions;
- rejected invalid actions for regression reproduction.

## P4C Decision

P4C does not implement final Asgard physics. It closes the product surface by documenting current runtime boundaries and adding isolation checks so Asgard cannot accidentally accept Rubik or Hodge actions.
