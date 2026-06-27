# P4G2 Phase 12 Recovery Note

Date: 2026-06-27

## Recovery State

The interrupted worktree was dirty and contained intended Phase 12 changes only:

- `tools/HetznerSignalRSmoke/Program.cs`
- `docs/NEXT_ERA_MICRO_RESEARCH_LOG.md`
- `docs/P4G2_CLASSIC_ONLINE_PLAY_RESULT.md`

No reset or stash was used. The existing Phase 12 work was preserved and completed.

## What Was Recovered

The smoke tool had already been moved away from the old Asgard-only wording and hardcoded action. The recovered work was finished by making action selection explicit:

- prefer `RequestLegalPreview` and submit a server-provided legal option;
- if the deployed hub does not expose `RequestLegalPreview`, use a documented compatibility fallback;
- fallback supports known-safe Classic/Single and Asgard smoke actions only;
- other profiles skip action submit honestly unless server preview is available.

## Smoke Results Repeated

Classic remote smoke over public HTTP 80 passed with the deployed-server compatibility fallback:

- profile: `classic-six-side-3d-8x8x8-v0.1`
- action source: `compat-fallback`
- action: `#1 S1 MOVE K (4,4,0)->(3,5,1)`

Asgard remote smoke over public HTTP 80 also passed:

- profile: `asgard-convergence-3d-8x8x8-v0.1`
- action source: `compat-fallback`
- action: `#1 S1 MOVE P (2,3,0)->(2,3,1)`

The deployed Hetzner service remains older than the local code at this point and reports `Method does not exist` for the newer `RequestLegalPreview` hub method.

## Boundary

This recovery did not touch Chess3D rules, native ABI, nginx, systemd, UFW, TLS/443, x-ui/Xray, Outline, Albatronix, or Unreal services.
