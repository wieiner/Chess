# P4C Portability And Product Surface Audit

Phase: P4C Phase 01

## Current Architecture Reality

The repository is a Windows-first game/product workspace with native C++ engines, WPF desktop apps, and a hosted ASP.NET Core SignalR server. P4B made online matchmaking and deployment templates real enough to test, but did not remove the core Linux runtime blocker.

## Linux Portability Findings

- ASP.NET Core itself can run on Linux behind Kestrel/Nginx, but this repository's server is not yet a plain portable ASP.NET Core app.
- `ChessOnlineServer` targets `net8.0-windows`.
- `ChessOnlineServer` copies `Chess3DEngine.dll` into output.
- `ChessOnlineProtocol` also targets `net8.0-windows`; it contains `OnlineGameSession`, which owns authoritative rules sessions.
- `ChessOnlinePersistence` targets `net8.0-windows` even though its code appears portable-shaped.
- WPF apps are correctly Windows-only and should not be part of the Linux server portability plan.
- Current native P/Invoke wrappers name Windows DLLs directly, for example `Chess3DEngine.dll`.

## Product Surface Findings

- Chess2D, Chess3D, RubikApp, online server, online app, AI/search, assets, and deployment docs are now all product-facing surfaces.
- Exactly five Chess3D RuleProfiles exist:
  1. `classic-six-side-3d-8x8x8-v0.1`
  2. `single-side-3d-8x8x8-v0.1`
  3. `asgard-convergence-3d-8x8x8-v0.1`
  4. `rubik-convergence-3d-8x8x8-v0.1`
  5. `hodge-projection-duel-3d-8x8x8-v0.1`
- Online matchmaking, deployment descriptors, regression fixtures, and playthroughs are not modes.
- Asgard/Rubik/Hodge are still visible in runtime tests and docs, but product docs are now broad enough that a consolidated feature matrix is needed.

## Documentation Contradictions / Staleness Risks

- Some docs say Linux deployment templates exist; they must continue to say runtime Linux is blocked until proven.
- Some docs contain many historical "Next" sections. P4C should consolidate roadmap/status in later phases.
- Online docs correctly avoid public production claims; keep that stance.
- Generated model pipeline docs exist for OBJ/MTL, but future generated figure import needs size/license/manifest validation.

## Safe Phase 01 Decision

No code refactor in Phase 01. The next technical decision should be Phase 02: choose the Linux portability path and define whether to split server transport/auth/persistence from Windows-native rules authority.

## References

- Microsoft Learn: Host ASP.NET Core on Linux with Nginx.
- Microsoft Learn: .NET RID Catalog and .NET application publishing overview.
- Microsoft Learn: ASP.NET Core SignalR hosting and scaling.
- Microsoft Learn: WPF overview, Windows-only UI framework.
- Microsoft Learn: .NET native library loading.
