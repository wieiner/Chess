# P4C Baseline Report

Date: 2026-06-17

## Repository State

- Branch: `main`
- Starting commit: `7e5ce76 Add Chess3D deployment and matchmaking MVP`
- Remote: `https://github.com/wieiner/Chess.git`
- Working tree before P4C Phase 00 docs: clean
- Previous GitHub Actions run: `27678188526`, `Windows Build`, success

## Baseline Verify

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Result: PASS.

Covered by the baseline verify:

- Release x64 build;
- development output asset checks;
- `ProductionOutput` packaging;
- Chess2D/Chess3D/Rubik/GPU/online contract tests;
- ChessOnline SignalR contract tests;
- Chess2DBenchmark quick smoke;
- no required CUDA boundary;
- representative no-secret packaging checks.

## Known P4B Limitations

- ChessOnlineServer is still a Windows-targeted server package (`net8.0-windows`).
- The authoritative online game path still depends on the Windows native `Chess3DEngine.dll`.
- Linux systemd/nginx files are deployment scaffolding, not proof of Linux runtime support.
- Matchmaking is an in-memory MVP; queued tickets are not durable.
- No real Hetzner/SSH deployment has been performed.
- No Redis, Azure SignalR, cloud backplane, ranked matchmaking, or public anti-cheat claims exist.

## Exact Next Phase

P4C Phase 01: portability and product surface audit.

The next phase should inspect project platform boundaries, Windows-only dependencies, product-facing modes/features, documentation contradictions, and the honest Linux portability path before code refactoring begins.
