# P4K Remote Online UX Smoke Tool

Date: 2026-07-13

## Purpose

`scripts/deploy/Test-HetznerOnlineUx.ps1` is the bounded operator entry point for public HTTP 80 ChessOnline UX smoke. It invokes `tools/HetznerSignalRSmoke` through the proven C# `TestProcessWatchdog`; remote smoke is manual/operator-only and is not a GitHub Actions requirement.

The deployment remains diagnostic/dev-only HTTP. Use generated temporary accounts only. This tool does not configure or touch nginx, UFW, TLS/443, x-ui/Xray, Outline, Albatronix, Unreal, PostgreSQL, or DNS.

## Scenarios

- `play`: health, temporary auth, SignalR, matchmaking, ready/start, snapshot, legal preview, optional accepted action, and action log.
- `resume`: `play`, disconnect/reconnect of the primary relay, then `RequestResumeMatch` with room/table/seat/ruleset/hash/sequence/history checks.
- `spectator`: `play`, temporary spectator authentication, `JoinSpectator`, read-only mutation rejection, and a live accepted-action update when action submit is enabled.
- `lobby`: lobby snapshot before matchmaking and after start, then exact room/table lookup with safe shortened player labels.
- `all`: deterministic `play`, `resume`, `lobby`, `spectator`, and final diagnostics.

Explicit `resume`, `spectator`, and `lobby` scenarios fail if diagnostics do not advertise their required feature flag and hub method. Missing capabilities are never converted into a local/fake PASS.

## Commands

Dry-run without build or network:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -Scenario all `
  -NoSecretLog `
  -DryRun
```

Remote all-scenario operator smoke:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Test-HetznerOnlineUx.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -Scenario all `
  -TimeoutSeconds 240 `
  -NoSecretLog `
  -BuildTool
```

Use `-SkipActionSubmit` only when a scenario intentionally verifies non-mutating connectivity. Resume and spectator positive proof normally require action submission.

## Output contract

The C# tool emits:

```text
STEP START <name>
STEP PASS <name>
STEP SKIP <name> reason=<sanitized>
STEP FAIL <name> reason=<sanitized>
SMOKE PASS scenario=<scenario>
SMOKE FAIL scenario=<scenario>
```

Default run IDs combine a UTC timestamp with a random suffix. Logs are unique and ignored by Git:

```text
.tmp/remote-ux-smoke/<runId>.stdout.<scenario>.log
.tmp/remote-ux-smoke/<runId>.stderr.<scenario>.log
```

`-RunId` is the canonical wrapper option; legacy `-UniqueRunId` remains an alias.

## Secret boundary

The tool keeps generated passwords and tokens in memory and never prints them. The wrapper redacts lines containing access/refresh token, authorization/bearer, password, private key, or connection-token markers before displaying log tails. Query strings are removed from displayed URLs. Raw `.tmp` logs must not be committed or posted publicly.

This follows [Microsoft SignalR security guidance](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) and the [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html): access tokens, passwords, connection tokens, and secret-bearing URLs do not belong in operator reports.

## Current deployed capability gate

The Phase 12 Hetzner package reported:

- `profileCount=5`;
- Linux native authority supported;
- `resumeMatch=true` and `RequestResumeMatch`;
- `spectatorMode=true` and `JoinSpectator`;
- `lobbySnapshot=true` and `RequestLobbySnapshot`.

Functional remote PASS is recorded separately by the scenario-specific P4K phases. Capability advertisement alone is not functional proof.
