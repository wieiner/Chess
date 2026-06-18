# Chess3D Deployment Decision Package

Status: P4C Phase 12.

This package records what can be deployed today, what is blocked, and what must remain future work. It does not perform a deployment and does not include real server IPs, private keys, certificates, tokens, runtime stores, Data Protection key rings, or operator secrets.

## Executive Decision

Use the Windows `ChessOnlineServer` package for executable hosted authority today.

Keep Hetzner Linux as the preferred production target, but treat it as blocked until P4D produces a Linux-native rules authority for the Chess3D engine and proves state-hash parity.

## Decision Matrix

| Option | Current status | When to use | Blocker / cost |
| --- | --- | --- | --- |
| Windows local/server package | executable now | demos, local hosted smoke, controlled Windows host | not the desired Linux VPS shape |
| Hetzner Linux native package | blocked | future production target | needs Linux `Chess3DEngine` native artifact and loader/parity tests |
| Linux gateway + Windows authority | future fallback | if Linux ingress is required before native authority | distributed authority protocol, latency, recovery complexity |
| Docker single-host | future packaging | after native Linux server works | container image, volumes, secrets, health policy |
| Kubernetes / orchestration | out of scope | much later scale-out | currently unnecessary and unsupported |
| Redis/Azure SignalR/backplane | out of scope | scale-out after product demand | not needed for P4C single-server MVP |
| Public ranked matchmaking | out of scope | after auth, persistence, anti-cheat, moderation | major product/security work |

## What Is Green Today

- Release x64 Windows build.
- Portable `ProductionOutput`.
- Windows `ChessOnlineServer` package.
- Windows start/stop/test scripts.
- SignalR authority contract tests.
- Auth/session/persistence JSON baseline.
- Exact-profile matchmaking smoke for Classic, Asgard, Rubik, and Hodge surfaces.
- CI artifact upload of `ProductionOutput`.

## Current Linux Blocker

The authoritative server path still depends on Windows runtime assets:

- `ChessOnlineServer` targets Windows-specific .NET output.
- `ChessOnlineProtocol` links the Windows native wrapper.
- `Chess3DEngine.dll` is copied as a Windows DLL, not a Linux `.so`.
- There is no Linux CI or state-hash parity test for all five RuleProfiles.

The nginx/systemd templates are useful deployment shape documents, not proof of Linux runtime readiness.

## Security And Persistence Rules

Never commit:

- real VPS IPs or hostnames tied to private infrastructure;
- SSH private keys;
- certificates or `.pfx`/`.pem`/`.key` files;
- production `appsettings.Production.json`;
- account/session stores;
- ASP.NET Core Data Protection key rings;
- logs with tokens/session IDs.

Operator-owned runtime paths should stay outside the repository and outside `ProductionOutput` source tracking.

## Recommended Next Gate

P4D should be a Linux-native authority spike:

- compile or package `Chess3DEngine` for Linux;
- split or adapt native loading for `linux-x64`;
- prove Classic/Single/Asgard/Rubik/Hodge state-hash parity against Windows;
- publish a Linux-compatible `ChessOnlineServer` package;
- only then run a controlled Hetzner probe.

