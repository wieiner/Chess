# P4F ChessOnlineApp Connection Panel

Date: 2026-06-27

This phase adds the first user-facing P4F surface to `ChessOnlineApp`: a server connection panel above the existing P3F hosted SignalR controls.

## What Was Added

The `P3F hosted SignalR transport` area now has a `Server Connection` block with:

- `Base URL` text box;
- `Use Hetzner HTTP` button;
- `Check Health` button;
- `Check Diagnostics` button;
- a visible status line.

The tracked default text remains a placeholder:

```text
http://<HETZNER_HOST>
```

The `Use Hetzner HTTP` button fills the current diagnostic endpoint for local operator use. This is intentionally HTTP 80 only; P4F does not touch 443, TLS, x-ui, Xray, Nginx, systemd, or firewall state.

## Health and Diagnostics

`Check Health` calls:

- `/healthz/live`;
- `/healthz/ready`.

It displays:

- live status;
- ready status;
- profile count;
- auth enabled flag;
- diagnostic HTTP warning when applicable.

`Check Diagnostics` calls:

- `/chess3d/diagnostics`.

It displays:

- auth enabled;
- authority platform;
- native authority library;
- authority supported flag;
- active connection count;
- room/table count;
- accepted/rejected action counts.

## SignalR Connect Behavior

The existing `Connect` button now normalizes the selected base URL through `ChessOnlineServerEndpoint` and connects to:

```text
/chess3d/relay
```

For backward compatibility, if the base URL box is still the placeholder, `Connect` falls back to the existing `Hub URL` text box. That keeps the older local P3F workflow working.

## Security Boundary

The panel shows:

```text
HTTP 80 is diagnostic/dev only. Do not enter real passwords until TLS is enabled.
```

No tokens or passwords are introduced in this phase. Auth and test-user UI are Phase 04.

## Current Limitations

- The panel checks health/diagnostics and connects SignalR, but it does not yet register/login users.
- `JoinMatchmaking` on the public server still needs authenticated clients; Phase 04 adds temporary auth.
- The UI still logs P3F protocol messages in the existing text log; a structured snapshot viewer comes later.

## Verification

Local checks for this phase:

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`
- `dotnet build src\ChessOnlineClient\ChessOnlineClient.csproj -c Release -p:Platform=x64`
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List`

