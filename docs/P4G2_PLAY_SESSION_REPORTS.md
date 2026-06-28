# P4G2 Play Session Reports

Date: 2026-06-28

## Purpose

`ChessOnlineApp` can save a short local play-session report for reproducing online UI and server-authority bugs. Reports are written under:

```text
.tmp/manual-smoke/p4f-online-client-session-YYYYMMDD-HHMMSS.json
```

The `.tmp` folder is local runtime output and must not be committed.

## Report Contents

The report includes:

- selected play mode;
- normalized endpoint and hub URL as shown in the UI;
- server status text and compact status text;
- selected ruleset id;
- room/table ids;
- short player id fragments;
- seat/turn summary;
- realtime sequence/resync summary;
- snapshot hash/action count/last notation;
- board ruleset/current side/current macro-player/occupied count;
- selected cell/from/to;
- legal-preview state hash, reason, option count, targets, and first action options;
- accepted/rejected action counters;
- UI status strings;
- action log tail;
- event log tail.

## Sanitized Clipboard Summary

The `Copy Sanitized Summary` button copies a concise text summary for bug reports. It is meant for chat/issues where a full JSON file would be too noisy.

## Secret Boundary

Reports and summaries intentionally omit:

- access tokens;
- refresh tokens;
- temporary passwords;
- authorization headers;
- private keys;
- keyrings;
- runtime stores.

Player ids are shortened, and authentication state is represented by high-level status only.

## Usage

1. Use `ChessOnlineApp` against the diagnostic HTTP 80 server.
2. Create temporary users only.
3. Start a match and request a snapshot.
4. Try a legal-preview action or reproduce the issue.
5. Click `Save Session Report`.
6. Optionally click `Copy Sanitized Summary`.

HTTP 80 remains diagnostic/dev-only. Do not enter real credentials.
