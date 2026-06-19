# P4D Baseline Report

Status: P4D phase 00 baseline.

## Repository State

- Repository: `E:\repos\mygames\Chess`
- Branch: `main`
- Start commit: `129c2716c0fd426dc9b8eee417cd38b9a1800558`
- Start commit message: `P4C phase 14: prepare Linux native authority spike`
- Working tree before P4D edits: clean
- Previous GitHub Actions run: `27750512819`
- Previous GitHub Actions result: success

## Baseline Verify

Baseline verification passed on the local machine with:

`CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT=1`

Command:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1`

Notes:

- An initial default verify attempt using the script default MSBuild max CPU count hit an early MSBuild exit during `Build Release x64`.
- A direct MSBuild retry with `/m:1` passed.
- Full `scripts\verify.ps1` then passed with `CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT=1`.
- This is treated as local MSBuild resource/contention behavior, not as a source regression.
- Test coverage was not reduced: build, packaging, contract tests, SignalR tests, and `Chess2DBenchmark --quick` still ran.

## Current Linux Blocker

The current hosted authority path is still Windows-native:

- `ChessOnlineServer` runs on the existing Windows package path.
- The rules authority still depends on the Windows `Chess3DEngine.dll` implementation.
- No Linux `libChess3DEngine.so` exists yet.
- No Linux native loading parity has been proven yet.

Local Windows-hosted Linux cross-build is blocked by missing Linux target sysroot:

- `C:\ll\sysroot`: missing
- `C:\ll\linux-sysroot`: missing
- `C:\ll\local\sysroot`: missing
- `C:\ll\local\x86_64-linux-gnu`: missing
- `SYSROOT`: unset
- `LINUX_SYSROOT`: unset

Because the sysroot is missing, P4D must not claim local Linux cross-compilation readiness. The safe next path is a native portability audit and, if needed, a controlled Hetzner native build probe.

## Local LLVM / Clang

Custom LLVM root:

`C:\ll\local\bin`

Detected tools:

- `clang.exe`
- `clang++.exe`
- `ld.lld.exe`
- `lld.exe`
- `llvm-ar.exe`
- `llvm-ranlib.exe`
- `llvm-readelf.exe`
- `llvm-objdump.exe`

## Hetzner Access Boundary

A Hetzner SSH access command was provided by the user for a future controlled probe. This tracked report intentionally uses placeholders and does not commit the real host, private key contents, `known_hosts`, tokens, passwords, certificates, runtime stores, databases, or keyrings.

Sanitized command shape:

`ssh -i "%USERPROFILE%\.ssh\id_ed25519_hetzner" root@<HETZNER_HOST>`

Real deployment remains out of scope for phase 00. A read-only probe is planned for a later P4D phase after local source checks and CI remain green.

## Scope Guard

P4D must not:

- add a sixth Chess3D RuleProfile;
- change Classic, Single-Side, Asgard, Rubik, or Hodge gameplay rules;
- weaken tests or `verify.ps1`;
- commit private keys, tokens, stores, keyrings, certificates, runtime databases, or raw SSH logs;
- introduce Redis, Azure SignalR backplane, Kubernetes, Docker orchestration, or public ranked matchmaking.
