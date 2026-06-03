# Chess3D Online Serialization

P3E serializes online messages as JSON using `OnlineProtocolJson`.

## Stable Data

The authority sends:

- protocol envelope metadata;
- room/table command DTOs;
- action command/event DTOs;
- authoritative snapshots;
- action-log chunks;
- diagnostics summaries.

## Runtime State

Board state is not hand-serialized by clients. The authoritative snapshot embeds existing Chess3D savegame JSON exported by `Chess3D_ExportSaveGameJson`, plus the deterministic state hash from `Chess3D_GetStateHash`.

## Backward Compatibility

Existing `.ch3dsave`, `.ch3dreplay`, action history, state hash, and native ABI remain unchanged. P3E adds managed DTOs and app-level packaging only.

## Deferred

Native online DTO exports, binary protocol, compression, cloud storage, and authenticated identity serialization are future work.
