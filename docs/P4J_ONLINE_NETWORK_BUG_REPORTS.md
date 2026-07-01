# P4J Online Network Bug Reports

Date: 2026-07-01

## Purpose

ChessOnlineApp can now save a focused network bug report for online play issues: reconnect gaps, resume failures, spectator/lobby mismatches, legal-preview dispatch problems, stale action logs, and server capability mismatches.

The report is for diagnostic/dev HTTP 80 operation. It is not a production telemetry pipeline.

## UI Controls

In `ChessOnlineApp` under the online relay panel:

- `Save Network Bug Report`
- `Copy Network Summary`

`Save Network Bug Report` writes:

```text
.tmp/manual-smoke/p4j-network-report-YYYYMMDD-HHMMSS.json
```

`Copy Network Summary` copies a short sanitized text summary to the clipboard.

## Report Contents

The JSON report includes:

- base URL and hub URL;
- last diagnostics/capabilities seen by `Check Diagnostics`;
- supported hub methods when available;
- Linux native authority capability summary;
- current play mode;
- room/table IDs;
- short player IDs only;
- reconnect state;
- realtime resync state;
- resume success/failure summary;
- spectator state;
- selected lobby row;
- legal-preview summary;
- accepted/rejected action counts;
- action-log tail;
- UI event-log tail after redaction.

## Redaction Boundary

The report builder redacts log lines containing:

- `accessToken`
- `refreshToken`
- `Authorization`
- `Bearer`
- `password`
- private-key-like names such as `id_ed25519`, `.pfx`, `.pem`, `.key`

The report does not intentionally include access tokens, refresh tokens, passwords, keyrings, runtime stores, certificates, or private keys.

Raw reports under `.tmp/manual-smoke` are local operator artifacts and must not be committed.

## Recommended Bug Reproduction Flow

1. Click `Use Hetzner HTTP`.
2. Click `Check Health`.
3. Click `Check Diagnostics`.
4. Create or join a test match.
5. Request snapshot and action log.
6. If the issue is about legal moves, click a source cell and request legal preview.
7. If the issue is about reconnect, disconnect/reconnect and then request snapshot/action log.
8. Click `Save Network Bug Report`.
9. Attach the sanitized JSON only after confirming it contains no credentials.

## Current Limitation

The current public Hetzner deployment may still lack Phase 18+ lobby/spectator hub methods until the server package is redeployed. In that case the network report will show missing capability flags instead of pretending that the lobby flow passed.
