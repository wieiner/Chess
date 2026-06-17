# Chess3D Asgard Online Test Matrix

Phase: P4C phase 08.

## Purpose

This matrix records how Asgard is protected in online authority tests, scenario descriptors, replay/save flows, and AI smoke. It is a planning and coverage document, not a new gameplay mode.

## Existing Coverage

| Area | Coverage | Files |
| --- | --- | --- |
| Profile catalog | exactly five Chess3D RuleProfiles | `tests/ChessOnlineContractTests/Program.cs`, `RuleProfileCatalog` |
| Online authority startup | Asgard table can start and emits authoritative snapshot | `tests/ChessOnlineContractTests/Program.cs` |
| SignalR matchmaking | matched Asgard table starts with authoritative snapshot | `tests/ChessOnlineSignalRContractTests/Program.cs` |
| Online fixture parsing | Asgard restore smoke descriptor is present and parseable | `assets/rules/scenarios/chess3d/online/online_asgard_restore_smoke_v0_1.json` |
| SignalR fixture parsing | Asgard restore SignalR descriptor is present and parseable | `assets/rules/scenarios/chess3d/signalr/signalr_asgard_restore_smoke_v0_1.json` |
| Native AI/search | Asgard AI candidates include reserve restore and do not mutate state | `tests/Chess3DEngineContractTests/Chess3DEngineContractTests.cpp` |
| AI regression descriptors | stack/fusion and reserve restore smoke descriptors exist | `assets/rules/scenarios/chess3d/regression/asgard_ai_*.json` |

## Phase 08 Added Coverage

| Check | Expected result |
| --- | --- |
| Submit `RubikLayerTurn` to base Asgard table | rejected as illegal action |
| Submit `HodgeProjectedMove` to base Asgard table | rejected as illegal action |
| Re-read snapshot after rejected action | state hash unchanged |
| Build Asgard AI candidate through online authority | candidate exists and remains profile-aware |

## Future Coverage Needed

- Save/load roundtrip fixture that explicitly asserts stack/fusion/reserve/anchor state after an Asgard action sequence.
- Replay fixture that reaches the same state hash after knockback, reserve restore, and core entry.
- Online reconnect fixture that asserts Asgard stack/reserve state remains visible in authoritative snapshot after reconnect.
- Future destructive fusion/implosion fixture, only after those rules are intentionally implemented.
- Contested-anchor scoring fixture, only after contested anchor policy is no longer deferred.

## Non-Goals

- No Redis/backplane durability in P4C.
- No Linux native engine claim in P4C.
- No final destructive Asgard physics.
- No Rubik/Hodge command acceptance in base Asgard.
