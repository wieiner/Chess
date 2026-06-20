# P4D1 Battle Linux Baseline

## Repository state

- Branch: `main`.
- Local `HEAD`: `758b11b06f7217258fb3616a06be83bb0497f4a3` (`P4D phase 11: plan Hetzner SignalR matchmaking smoke`).
- `origin/main`: `758b11b06f7217258fb3616a06be83bb0497f4a3` after pushing the pending Phase 11 commit.
- Working tree: clean at baseline capture.

## CI state

- Previous confirmed GitHub Actions run before P4D1: `27866919077`.
- Result: success.
- Scope: Windows Build, including verify/package/contract tests and portable artifact upload.

## Local verification

- `git diff --check`: passed before pushing pending Phase 11.
- `tests/run-tests.ps1 -SkipSolutionBuild -SkipBenchmark`: passed after clearing stale local MSBuild worker processes.
- `scripts/verify.ps1`: passed with `CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT=1`.

Local note: a previous parallel MSBuild attempt left many `MSBuild.exe /nodemode` worker processes and caused a transient build failure. A direct `/m:1` solution build passed, and full verify passed with the same single-MSBuild guard.

## Current blockers

1. Hetzner Ubuntu host is reachable by SSH, but the read-only probe showed missing build/runtime tools:
   - `.NET SDK/runtime` missing;
   - `clang++` missing;
   - `cmake` missing;
   - `ninja` missing;
   - `nginx` missing.
2. `libChess3DEngine.so` has not been built yet.
3. `ChessOnlineServer` linux-x64 publish is still blocked by the current Windows-targeted server graph and missing Linux native library.
4. Local Windows-to-Linux cross-build remains blocked by lack of Linux sysroot for the custom LLVM toolchain.
5. No Kestrel/systemd/Nginx smoke has been run yet because no Linux package exists.

## Planned battle path

1. Install Hetzner build/runtime prerequisites after a fresh read-only check.
2. Build `libChess3DEngine.so` natively on Hetzner with the CMake scaffold.
3. Verify Linux ABI exports against the expected Chess3D C ABI.
4. Remove the Windows-only server publish blocker without changing native DTO layout or game rules.
5. Produce a `linux-x64` ChessOnlineServer package.
6. Copy only the package and safe templates to a temporary Hetzner smoke path.
7. Run Kestrel-only health/diagnostics/auth/matchmaking/Asgard smoke.
8. Only after Kestrel smoke, add systemd and Nginx steps.

## Safety boundaries

- No sixth Chess3D RuleProfile.
- No rule changes for Classic, Single, Asgard, Rubik, or Hodge.
- No Redis, Kubernetes, Docker orchestration, or new backplane in this phase.
- No private keys, known_hosts, tokens, passwords, certificates, runtime stores, keyrings, DB files, sysroot archives, or smoke artifacts committed.
- Tracked documentation should use `<HETZNER_HOST>` instead of the real host address.
