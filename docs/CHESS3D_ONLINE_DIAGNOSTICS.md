# Chess3D Online Diagnostics

`OnlineDiagnostics` reports lightweight authority counters.

## Counters

- rooms;
- tables;
- players;
- accepted actions;
- rejected actions;
- snapshots served;
- action-log chunks served.

## Use

Diagnostics are exposed through the local `ChessOnlineApp` panel and contract tests. They help confirm that rejected actions stay rejected and accepted actions advance `serverSeq`.

## Limits

Diagnostics are not monitoring, telemetry, or production observability. Hosted metrics, distributed tracing, rate-limit counters, and audit logs are future work.
