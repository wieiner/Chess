# P4G2 Secret and Log Audit

Date: 2026-06-28

## Scope

This audit covers the P4G2 actual-online-play client, smoke tooling, reports, and docs. It does not change Chess3D rules, server deployment, Nginx, UFW, TLS, port 443, x-ui/Xray, Outline, Albatronix, or Unreal services.

## Commands

```powershell
rg -n "accessToken|refreshToken|Authorization|Bearer|password|privateKey|shortId|id_ed25519|x-ui|xray|178\.105\.220\.117" src tests tools scripts docs
git check-ignore .tmp\manual-smoke\dummy.json
git check-ignore .tmp\test-logs\dummy.log
git check-ignore DeploymentOutput\dummy.txt
git check-ignore ProductionOutput\dummy.txt
```

## Findings

The scan found expected references in these categories:

- DTO/property names such as `AccessToken`, `RefreshToken`, `Authorization`, and `Bearer`.
- Server-side account/session code that creates and hashes tokens/passwords.
- Client-side runtime variables for temporary passwords and tokens.
- Contract tests that intentionally verify redaction.
- Smoke-tool code that passes tokens to SignalR through an access-token provider.
- Documentation warning that HTTP 80 is diagnostic/dev-only.
- Out-of-scope service warnings for x-ui/Xray/443.
- The public Hetzner HTTP endpoint in operator commands and diagnostic docs.

No tracked runtime token file, private key, certificate, keyring, raw store, manual smoke report, or test log was found.

## Ignore Coverage

The following paths are ignored by Git:

- `.tmp/manual-smoke/...`
- `.tmp/test-logs/...`
- `DeploymentOutput/...`
- `ProductionOutput/...`

These are the expected locations for runtime session reports, watchdog/test logs, deployment packages, and generated production output.

## P4G2 Session Reports

`ChessOnlineApp` session reports are local `.tmp` artifacts. They include diagnostic state such as ruleset id, room/table id, state hash, legal-preview count, realtime sequence, and action-log tail. They do not include:

- access tokens;
- refresh tokens;
- temporary passwords;
- authorization headers;
- private keys;
- keyrings;
- runtime stores.

The `Copy Sanitized Summary` button uses shortened player ids and explicit `tokens=redacted` / `passwords=redacted` markers.

## Public IP Policy

The public HTTP endpoint remains present in operator-facing docs and scripts because the current development deployment is intentionally reachable over HTTP 80 for diagnostics. It is not a credential. The docs continue to mark HTTP 80 as diagnostic/dev-only and warn against real passwords.

## Decision

Keep remote smoke and manual UI smoke as operator-driven activities, not CI-required steps. Continue using temporary users only until a future TLS/domain phase.
