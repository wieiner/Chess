# P4J Secret and Logging Audit

Date: 2026-07-01

## Scope

This audit covers P4J online resume, spectator, lobby, and network-report UI changes.

It does not change Chess3D rules, server deployment, TLS/443, x-ui/Xray, Outline, Albatronix, Unreal, nginx, systemd, UFW, or firewall state.

## Commands

```powershell
rg -n "accessToken|refreshToken|Authorization|Bearer|password|privateKey|id_ed25519|\.pfx|\.pem|\.key|x-ui|xray|178\.105\.220\.117|manual-smoke|test-logs|keyring|runtime store" src tests tools scripts docs .gitignore
git check-ignore .tmp\manual-smoke\dummy.json .tmp\test-logs\dummy.log .tmp\runtime-store\store.json
git ls-files | rg -n "(\.tmp|manual-smoke|test-logs|keyring|\.pfx$|\.pem$|\.key$|\.sqlite$|\.db$|store\.json|token|password)"
```

## Findings

The search found expected source identifiers and documentation warnings:

- DTO/runtime names such as `AccessToken`, `RefreshToken`, `Authorization`, and `Bearer`.
- Auth client/server code that creates temporary passwords, hashes passwords, and handles access/refresh tokens in memory.
- Contract tests that deliberately inject secret-like strings to prove redaction.
- Smoke tooling variables for generated temporary users and passwords.
- Documentation that warns about HTTP 80, x-ui/Xray/443, runtime stores, keyrings, and private keys.

No tracked raw access token, refresh token, private key, certificate, runtime database, runtime store, Data Protection keyring, or manual smoke report was found by the tracked-file filter.

## Ignored Runtime Paths

`git check-ignore` confirms these local artifact paths are ignored:

- `.tmp/manual-smoke/dummy.json`
- `.tmp/test-logs/dummy.log`
- `.tmp/runtime-store/store.json`

`.gitignore` also excludes:

- `ProductionOutput/`
- `DeploymentOutput/`
- `.tmp/`
- logs/temp/dumps
- packaged archives
- secret-like extensions such as `.key`, `.pfx`, and `.pem` through verify checks

## P4J Report Redaction

`ChessOnlineApp` network reports are saved under:

```text
.tmp/manual-smoke/p4j-network-report-YYYYMMDD-HHMMSS.json
```

The network report builder:

- includes server capability flags and supported hub methods;
- includes reconnect/resume/spectator/lobby/action-log diagnostics;
- uses short player IDs only;
- redacts log lines containing token/password/Authorization/Bearer/private-key-like terms;
- marks `tokensRedacted`, `refreshTokensRedacted`, `passwordsRedacted`, and `authorizationHeadersRedacted`.

Raw reports remain local operator artifacts and must not be committed.

## Public HTTP Boundary

The public IP appears in operator-facing commands and docs because the current ChessOnline deployment is intentionally available over HTTP 80 for diagnostics/dev testing.

HTTP 80 remains diagnostic-only:

- use temporary users only;
- do not enter real passwords;
- TLS/domain/443 remain deferred;
- x-ui/Xray/Outline/Albatronix/Unreal are out of scope.

## Result

Status: PASS.

No P4J tracked change introduces committed credentials or raw runtime artifacts.
