# Chess3D Online Diagnostics

`OnlineDiagnostics` reports lightweight authority counters.

## Counters

- rooms;
- tables;
- players;
- accepted actions;
- rejected actions;
- active connections;
- resync requests;
- snapshots served;
- action-log chunks served.

## Use

Diagnostics are exposed through the local `ChessOnlineApp` panel and contract tests. They help confirm that rejected actions stay rejected and accepted actions advance `serverSeq`.

P3F also exposes diagnostics through `/chess3d/diagnostics` and the SignalR `Diagnostics` hub method. Session tokens and secrets are intentionally omitted.

## Limits

Diagnostics are not monitoring, telemetry, or production observability. Hosted metrics, distributed tracing, rate-limit counters, and audit logs are future work.
