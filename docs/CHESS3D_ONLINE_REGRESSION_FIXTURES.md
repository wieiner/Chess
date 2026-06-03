# Chess3D Online Regression Fixtures

Online fixture JSON files live under:

`assets/rules/scenarios/chess3d/online`

They are protocol descriptors used by `ChessOnlineContractTests`.

## Fixtures

- `online_protocol_hello_v0_1.json`
- `online_room_create_join_v0_1.json`
- `online_table_classic_start_v0_1.json`
- `online_classic_move_accept_v0_1.json`
- `online_classic_wrong_actor_reject_v0_1.json`
- `online_stale_hash_resync_v0_1.json`
- `online_asgard_restore_smoke_v0_1.json`
- `online_rubik_layer_turn_smoke_v0_1.json`
- `online_hodge_composite_smoke_v0_1.json`
- `online_snapshot_roundtrip_v0_1.json`
- `online_action_log_replay_hash_v0_1.json`
- `online_reconnect_resync_v0_1.json`
- `online_malformed_message_reject_v0_1.json`
- `online_unknown_future_field_tolerated_v0_1.json`

All fixtures use `format = chess3d-online-regression`.

## Purpose

The fixtures document protocol expectations and guarantee that online smoke assets are copied into development output and `ProductionOutput`.
