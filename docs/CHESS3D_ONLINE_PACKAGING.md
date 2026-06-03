# Chess3D Online Packaging

P3E packages online assets with `ChessOnlineApp`. P3F also packages a local hosted `ChessOnlineServer` prototype.

## Development Output

`src/ChessOnlineApp/bin/x64/Release/net8.0-windows` contains:

- `Assets/Rules3D/Profiles`
- `Assets/Rules3D/Online`
- `Assets/Rules3D/OnlineScenarios`

`src/ChessOnlineServer/bin/x64/Release/net8.0-windows` contains:

- `ChessOnlineServer.exe`
- `Assets/Rules3D/Profiles`
- `Assets/Rules3D/Online`
- `Assets/Rules3D/OnlineScenarios`
- `Assets/Rules3D/SignalRScenarios`

## Production Output

`ProductionOutput/ChessOnlineIntegrations` carries the same profile, protocol schema, and online scenario assets.

`ProductionOutput/ChessOnlineServer` carries the hosted server executable, profile assets, protocol schema assets, online scenario descriptors, and SignalR scenario descriptors.

## Verify Checks

`scripts/verify.ps1` checks representative online files in both development and portable output:

- `classic_six_side_3d_v0_1.json`
- `hodge_projection_duel_3d_v0_1.json`
- `chess3d_relay_v0_1.schema.json`
- `online_protocol_hello_v0_1.json`
- `online_hodge_composite_smoke_v0_1.json`
- `signalr_hello_connect_v0_1.json`

The checks intentionally do not require `rude-resource` or cloud credentials.

The root portable package also includes `run_chess_online_server.bat` for local hosted transport smoke tests.
