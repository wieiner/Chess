# P4J Phase 09R - Resume Baseline Check

Date: 2026-07-01

## Local Repository

- Branch: `main`
- HEAD: `34e8101f9881d48fd42437cedf73454c616f9807`
- Commit: `P4J phase 09: add client resume support`
- `origin/main`: `34e8101f9881d48fd42437cedf73454c616f9807`
- Working tree before this doc: clean.

Latest GitHub Actions before Phase 09R:

- Run id: `28359575909`
- Workflow: `Windows Build`
- Result: `success`
- Commit: `P4J phase 09: add client resume support`

## Hetzner HTTP 80 Health

The public diagnostic deployment on `http://178.105.220.117` responded:

- `/healthz/live`: `Healthy`
- `/healthz/ready`: ready JSON with `profileCount=5`, `authEnabled=true`, `persistenceProvider=json`
- `/chess3d/diagnostics`: Linux native authority is supported and uses `libChess3DEngine.so`

The diagnostics still report exactly five Chess3D rule profiles through the ready endpoint. No sixth profile was added or inferred.

## Resume Capability Status

Local repo status:

- client DTOs exist;
- server `RequestResumeMatch` exists in local source;
- `ChessOnlineRelayClient.RequestResumeMatchAsync` exists;
- `ChessOnlineApp` has `Resume Current Match`;
- sanitized session reports include non-secret resume context.

Current Hetzner deployment status:

- `requestLegalPreview=true`
- `realtimeResync=true`
- `actionLog=true`
- `matchmaking=true`
- `resumeMatch` is not present in the returned diagnostics JSON;
- `supportedHubMethods` does not include `RequestResumeMatch`.

Conclusion: local Phase 08/09 resume code is ahead of the currently deployed Hetzner server package. Resume UI can be built locally, but a real remote resume smoke requires deploying a server package that includes `RequestResumeMatch`, or it will fail with a missing hub method.

## Build And Checks

Local checks performed:

- `dotnet build src\ChessOnlineApp\ChessOnlineApp.csproj -c Release -p:Platform=x64`: PASS
- `dotnet build src\ChessOnlineClient\ChessOnlineClient.csproj -c Release -p:Platform=x64`: PASS after clearing a local `VBCSCompiler` file lock
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\tests\run-tests.ps1 -List`: PASS
- `git diff --check`: PASS

The first parallel client/app build attempt hit a local compiler-server file lock on `ChessOnlineProtocol` generated files. Re-running sequentially after stopping `VBCSCompiler` passed and did not require code changes.

## Boundary

This phase did not touch:

- 443/TLS/domain;
- x-ui/Xray;
- Outline;
- Albatronix Docker;
- Unreal SYServer;
- nginx/systemd/UFW/firewall;
- Chess3D rules or profile JSON.

HTTP 80 remains diagnostic/dev only, with temporary users only.
