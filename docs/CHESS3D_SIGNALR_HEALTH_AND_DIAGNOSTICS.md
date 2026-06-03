# Chess3D SignalR Health And Diagnostics

## Endpoints

```text
GET /healthz/live
GET /healthz/ready
GET /chess3d/diagnostics
```

## Hub Diagnostics

The `Diagnostics` hub method returns `OnlineDiagnostics` through the standard protocol envelope.

Counters include:

- room count;
- table count;
- started table count;
- action log count;
- active connection count;
- accepted action count;
- rejected action count;
- resync count.

## Privacy Boundary

Diagnostics are for local development. They must not expose session tokens or secrets.

