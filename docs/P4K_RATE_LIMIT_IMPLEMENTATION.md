# P4K Bounded Online Request Limits

Date: 2026-07-13

## Status

Phase 39 is implemented and locally verified. It is not deployed by this
commit. Deployment remains gated by Phase 40 readiness hardening and the
guarded Phase 41 package workflow.

## HTTP Policies

The server uses named ASP.NET Core fixed-window policies with queue limit zero:

| Surface | Default budget | Partition |
| --- | --- | --- |
| register | 5 per 600 seconds | remote IP |
| login | 10 per 60 seconds | remote IP |
| refresh/logout/me | 30 per 60 seconds | authenticated player, otherwise IP |
| public diagnostics | 30 per 60 seconds | remote IP |

`/healthz/live` and `/healthz/ready` remain outside these restrictive policies
so operator and service monitoring cannot consume the auth budget. Rejections
return HTTP 429 and fixed secret-free JSON with `errorCode=rateLimited`.

All values are configuration-backed through `HostedOnline:RateLimits`. The
defaults are intentionally conservative for the temporary-user HTTP 80
diagnostic deployment and may be tuned only after observing bounded smoke/load
results.

## SignalR Guard

The existing global hub command ceiling remains a defense-in-depth guard. Its
partition is now the normalized authenticated `PlayerId`, so reconnecting with
a new transport `ConnectionId` does not reset the budget. Anonymous development
sessions retain a connection-scoped fallback. A rejected command returns the
append-only protocol reason `rateLimited` before registry/native mutation.

The guard has an injected `TimeProvider`; expired buckets are removed during
subsequent checks. Partition keys are not logged or returned to clients.

## Logging Boundary

`Microsoft.AspNetCore.Hosting` and `Microsoft.AspNetCore.Http.Connections` are
filtered below `Warning`. This closes the Phase 37 risk where framework Info
request logs could include a SignalR `access_token` query string. Application
errors still use fixed client text and server-side structured logging.

## Verification

Targeted `ChessOnlineSignalRContractTests` passed under the C# process watchdog
in 34 seconds. The tests prove:

- allowed registration and diagnostics requests succeed;
- the next burst request receives HTTP 429;
- the 429 body contains no password or token text;
- health remains usable after the diagnostics budget is exhausted;
- authenticated players receive independent partitions;
- reconnect cannot reset one player's budget;
- a fake-clock window renewal succeeds;
- partition keys use player ID or IP and never query tokens;
- normal SignalR gameplay, matchmaking, persistence, and profile isolation
  remain green.

## Operational Boundary

This is application-level protection, not transport confidentiality or a
distributed denial-of-service boundary. HTTP 80 remains diagnostic/dev only,
temporary users remain mandatory, and no Redis/backplane or proxy/network
configuration was introduced. Exactly five rule profiles remain unchanged.
