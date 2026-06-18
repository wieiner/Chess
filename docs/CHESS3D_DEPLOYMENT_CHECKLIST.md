# Chess3D Deployment Checklist

Status: P4C Phase 12.

Use this checklist before claiming any deployment target is ready.

## Repository Safety

- [ ] `git status --short` is clean.
- [ ] No `appsettings.Production.json` is tracked.
- [ ] No `.pem`, `.pfx`, `.key`, certificate, token, or private SSH key is tracked.
- [ ] No runtime store or keyring is tracked.
- [ ] `rude-resource/` remains ignored and untouched as tracked content.
- [ ] Real VPS IPs and private operator paths are not committed.

## Build And Package

- [ ] `scripts/verify.ps1` passes locally.
- [ ] GitHub Actions Windows Build passes.
- [ ] `ProductionOutput/ChessOnlineServer` exists.
- [ ] Windows deploy scripts are present in the package.
- [ ] Linux templates are present as templates only.
- [ ] Generated asset manifests are copied without heavy generated mesh binaries.

## Windows Server Path

- [ ] `ChessOnlineServer.exe` starts from `ProductionOutput/ChessOnlineServer`.
- [ ] `/healthz/live` returns success.
- [ ] `/healthz/ready` returns success.
- [ ] SignalR contract smoke passes.
- [ ] Store and Data Protection keyring are in operator-owned runtime paths.
- [ ] Backup path is known before updates.

## Linux / Hetzner Readiness

- [ ] Linux-native `Chess3DEngine` exists.
- [ ] Native loading is verified for `linux-x64`.
- [ ] `ChessOnlineServer` can publish/run for Linux.
- [ ] State-hash parity passes for all five RuleProfiles.
- [ ] systemd service template is adapted outside Git with real host paths.
- [ ] nginx template is adapted outside Git with real hostname.
- [ ] TLS/private keys are created outside Git.

If any Linux readiness item is unchecked, do not claim a Linux production deployment.

## Online Product Scope

- [ ] Public matchmaking is explicitly marked MVP/single-server or future.
- [ ] Redis/Azure SignalR/backplane is marked future.
- [ ] Anti-cheat is not overstated.
- [ ] Asgard/Rubik/Hodge profile gates are still enforced.
- [ ] Scenario/playthrough JSON files are not counted as game modes.

