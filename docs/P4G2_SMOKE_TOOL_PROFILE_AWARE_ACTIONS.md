# P4G2 Smoke Tool Profile-Aware Actions

Date: 2026-06-27

## Purpose

`tools/HetznerSignalRSmoke` is an operator smoke tool for the public HTTP diagnostic deployment. It must not assume that every profile is Asgard, and it must not silently submit an Asgard-specific move when testing Classic.

## Action Selection

The tool now uses this order:

1. Start a match for the requested `ProfileId`.
2. Request the authoritative snapshot.
3. Parse `OnlineSnapshot.SaveGameJson` through the client board snapshot adapter.
4. Try the server `RequestLegalPreview` hub method.
5. If preview succeeds, submit the first submit-ready legal option and print `action-source=server-preview`.
6. If the deployed hub does not yet have `RequestLegalPreview`, use an explicit compatibility fallback and print `action-source=compat-fallback`.

## Compatibility Fallbacks

Fallbacks are intentionally limited:

- Classic/Single-Side: `NormalMove S1 K (4,4,0)->(3,5,1)`.
- Asgard: `NormalMove S1 P (2,3,0)->(2,3,1)` unless custom `--from-*` / `--to-*` CLI values are passed.

Rubik, Hodge, and other special-action flows skip action submit if server preview is unavailable. This avoids mapping special actions to `NormalMove` accidentally.

## Current Hetzner State

The public Hetzner service currently passes health/auth/SignalR/matchmaking/start/action smoke, but the deployed hub does not expose the newer `RequestLegalPreview` method yet. Phase 13+ will audit and deploy the updated server package.

## Security Boundary

The smoke uses random temporary users. It does not print access tokens, refresh tokens, or generated passwords. HTTP 80 remains diagnostic/dev-only until a separate TLS/domain deployment decision is made.
