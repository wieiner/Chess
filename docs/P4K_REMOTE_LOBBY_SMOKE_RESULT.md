# P4K Remote Lobby Smoke Result

Date: 2026-07-13

## Result

Public Hetzner active-table discovery passed for the Asgard profile.

- run ID: `p4k-phase19-asgard-lobby-20260713`;
- profile: `asgard-convergence-3d-8x8x8-v0.1`;
- result: `SMOKE PASS`;
- smoke duration: approximately 1.54 seconds;
- watchdog process duration: approximately 4.53 seconds.

The initial filtered lobby was not empty: it contained five active Asgard tables created by earlier temporary operator smoke runs. The scenario then created `match-8-asgard` / `table-8`, started it, submitted one legal server-preview move, and located that exact table in a fresh lobby snapshot.

## Selected row

The selected row passed checks for:

- exact `roomId` and `tableId`;
- ruleset `asgard-convergence-3d-8x8x8-v0.1`;
- lifecycle state `InGame`;
- occupied seats `2`;
- maximum seats `6`;
- non-negative spectator count (`0` in the current best-effort implementation);
- `started=true`;
- last server sequence `1`, matching the authoritative snapshot;
- parseable `updatedUtc`;
- shortened player labels of at most eight characters.

## Privacy check

The serialized selected row contained none of these field names:

- access/refresh token;
- Authorization;
- connection ID;
- password;
- email;
- private key;
- runtime store;
- session token.

No full player identifiers, credentials, or transport identifiers were printed.

## Row-driven actions

The exact selected row was used as the source for two follow-up operations:

1. the matching seated player resumed using the row's room/table/ruleset and selected seat; the server returned the same authoritative state hash;
2. a third temporary authenticated user joined the same row as a read-only spectator and received the same authoritative snapshot.

After both operations, a fresh player snapshot retained the pre-discovery state hash and action count. Lobby discovery, same-connection resume, and spectator join did not mutate gameplay state.

## Known limitation

`spectatorCount` is currently a best-effort placeholder (`0`) as documented by the server lobby warning. Exact spectator membership/count lifecycle is deferred to P4K spectator-registry phases; this smoke verifies the field is safe and non-negative, not that it is durable or exact.

Raw logs remain below ignored `.tmp/remote-ux-smoke/`. HTTP 80 remains diagnostic/dev-only and temporary users only.
