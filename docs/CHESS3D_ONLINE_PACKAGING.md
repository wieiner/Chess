# Chess3D Online Packaging

P3E packages online assets with `ChessOnlineApp`.

## Development Output

`src/ChessOnlineApp/bin/x64/Release/net8.0-windows` contains:

- `Assets/Rules3D/Profiles`
- `Assets/Rules3D/Online`
- `Assets/Rules3D/OnlineScenarios`

## Production Output

`ProductionOutput/ChessOnlineIntegrations` carries the same profile, protocol schema, and online scenario assets.

## Verify Checks

`scripts/verify.ps1` checks representative online files in both development and portable output:

- `classic_six_side_3d_v0_1.json`
- `hodge_projection_duel_3d_v0_1.json`
- `chess3d_relay_v0_1.schema.json`
- `online_protocol_hello_v0_1.json`
- `online_hodge_composite_smoke_v0_1.json`

The checks intentionally do not require `rude-resource` or cloud credentials.
