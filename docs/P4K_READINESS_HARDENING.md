# P4K Online Readiness Hardening

Date: 2026-07-13

## Result

`/healthz/live` remains a simple process liveness endpoint. `/healthz/ready`
now verifies the concrete dependencies required to accept an online chess
session instead of checking only that five filenames exist.

## Readiness Dependencies

The readiness probe requires all of the following:

1. normalized HTTP/HTTPS host, hub, profile, persistence, keyring, and receive
   size configuration;
2. the catalog contains exactly five profiles and the profile directory has
   exactly the five expected `*_3d_v0_1.json` files;
3. native runtime diagnostics report support and a disposable native session
   can be created with a non-empty authoritative state hash;
4. `OnlineRoomRegistry` is initialized and its diagnostics are readable;
5. the persistence directory accepts a bounded create/flush/delete marker;
6. the Data Protection keyring directory accepts the same bounded marker.

The write probes do not inspect, modify, enumerate, or report existing store or
key files. The marker uses a random name and is removed in `finally`.

## Public Contract

Healthy response keeps the existing append-only fields:

```json
{
  "status": "ready",
  "protocolId": "...",
  "protocolVersion": "...",
  "profileCount": 5,
  "authEnabled": true,
  "persistenceProvider": "json"
}
```

Failure returns HTTP 503 with `status=notReady` and one or more fixed reason
codes:

- `configurationInvalid`;
- `profileSetInvalid`;
- `nativeUnavailable`;
- `registryUnavailable`;
- `persistenceUnavailable`;
- `keyRingUnavailable`.

The response never includes absolute paths, key names, permission text,
exception messages, stack traces, or native loader paths.

## Configuration Correction

The tracked production sample previously used the obsolete root section
`ChessOnlineServer`. Runtime binding uses `HostedOnline`, as do development and
local samples. Phase 40 corrects the production sample and documents the new
rate-limit defaults. This does not change deployment-local configuration or
runtime data.

## Tests

`ChessOnlineSignalRContractTests` verifies:

- healthy readiness through the actual local server/native authority;
- missing native runtime;
- wrong profile set;
- unwritable persistence fixture;
- unavailable keyring fixture;
- safe aggregate failure JSON;
- existing health, auth, SignalR, persistence, matchmaking, and five-profile
  contracts remain green.

Phase 40 changes server code but does not deploy it. Phase 41 retains the
package hash, backup, health gate, remote smoke, and rollback requirements.
