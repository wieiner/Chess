# P4G2 Phase 12 - Classic Online Play Result

Date: 2026-06-27

## Scope

Phase 12 verifies that Classic Six-Side can be played through the public HTTP diagnostic deployment, not only through the earlier Asgard smoke. It does not add a mode, change Chess3D rules, touch TLS/443, or modify the Hetzner service deployment.

## Tooling Change

`tools/HetznerSignalRSmoke` now uses profile-neutral labels and attempts to build its submitted action from the online legal-preview contract:

1. Parse the authoritative `OnlineSnapshot.SaveGameJson`.
2. Enumerate occupied cells for the current side.
3. Invoke `RequestLegalPreview`.
4. Submit the selected legal action option.

The currently deployed Hetzner server is older than the latest client tooling and returns `HubException: Method does not exist` for `RequestLegalPreview`. For that deployed version only, the smoke tool falls back to versioned known-safe actions and prints `action-source=compat-fallback`:

- Classic/Single-Side: `NormalMove S1 K (4,4,0)->(3,5,1)`.
- Asgard/Rubik default legacy path: the existing `NormalMove S1 P (2,3,0)->(2,3,1)`.
- Rubik/Hodge special action fallback is not submitted without server preview.

This keeps the public smoke useful while preserving the new legal-preview path for newer server deployments.

## Classic Remote Smoke

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "classic-six-side-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog `
  -BuildSmokeTool
```

Result: PASS.

Observed path:

- health live/ready: PASS;
- diagnostics: available through the wrapper health phase;
- register/login two temporary users: PASS;
- SignalR connect: PASS;
- matchmaking: `match-10-classic`, `table-10`;
- game start: PASS, initial state hash `525757d8c254b919`;
- legal-preview hub unavailable on deployed server, `action-source=compat-fallback` selected;
- submitted action: `#1 S1 MOVE K (4,4,0)->(3,5,1)`;
- final snapshot/action log: PASS, final hash `679085fef5801b2a`.

## Asgard Regression Smoke

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\deploy\Test-HetznerSignalRMatchmaking.ps1 `
  -BaseUrl "http://178.105.220.117" `
  -ProfileId "asgard-convergence-3d-8x8x8-v0.1" `
  -TimeoutSeconds 180 `
  -NoSecretLog `
  -BuildSmokeTool
```

Result: PASS.

Observed path:

- matchmaking: `match-11-asgard`, `table-11`;
- game start hash `a0296f7e94a22346`;
- deployed hub legal-preview unavailable, `action-source=compat-fallback` selected;
- submitted action: `#1 S1 MOVE P (2,3,0)->(2,3,1)`;
- final hash `1116b19374131cc4`.

## Boundary

HTTP 80 remains diagnostic/dev-only. The smoke uses random temporary users and does not log access tokens or passwords. TLS/domain/443 remains deferred.

Remote smoke is an operator check, not a GitHub Actions requirement.
