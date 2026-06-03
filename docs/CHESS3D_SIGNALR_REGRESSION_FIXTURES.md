# Chess3D SignalR Regression Fixtures

SignalR fixture descriptors live under:

```text
assets/rules/scenarios/chess3d/signalr
```

Format:

```text
chess3d-signalr-regression
```

## Covered Scenarios

- hello/connect;
- room create/join;
- table start in Classic;
- Classic accepted action broadcast;
- wrong actor reject;
- stale hash resync;
- reconnect snapshot;
- action log chunk;
- Rubik layer turn;
- Hodge composite action;
- Asgard restore smoke;
- duplicate seat race;
- parallel submit monotonic server sequence;
- malformed message reject;
- diagnostics without secrets.

The fixtures are descriptor-backed tests, not new RuleProfiles.

