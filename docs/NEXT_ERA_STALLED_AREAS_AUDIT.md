# Next Era Stalled Areas Audit

Date: 2026-06-21

Scope: audit only. This document does not add modes, rules, deployment services, external integrations, or secrets. It records where work is genuinely stalled or deferred after the Next Era Linux dry-run and Chess2D portal audit.

## Repository Shape

| Check | Result |
| --- | --- |
| Local branch | `main` |
| Remote branches | `origin/main` only |
| Tags | none found |
| Remote heads | `refs/heads/main` only |
| Current commit at audit time | `4b2d03cd NextEra phase 10: audit Chess2D portal integration path` |
| History shape | Linear phase history on `main`; no hidden release branch discovered. |

There is no separate stalled branch or tag line to recover. The stalled work is mostly documented future scope inside `docs`, `assets/rules/profiles`, deploy templates, and explicit runtime limitations.

## Build / Test Runner Debt

Status: improved, but still operational debt.

Completed:

- decomposed `tests/run-tests.ps1` with suites, `-Only`, controlled `/m:N`, per-test timeouts, and logs;
- C# `TestProcessWatchdog` is the trusted timeout wrapper;
- CI Windows Build is green on `main`;
- `Chess2D`, `Chess3D`, online, SignalR, and packaging gates are covered by the Windows workflow.

Still stalled/deferred:

- no Linux CI job builds `libChess3DEngine.so` from a clean Linux environment;
- no CI job performs Linux `ChessOnlineServer` publish/smoke;
- no automated public HTTP/SignalR smoke after Nginx/systemd changes;
- no automated TLS/domain smoke;
- Node.js 20 deprecation warning still appears for GitHub actions forced to Node 24 by the runner environment.

Recommended next step:

- add a separate, explicit Linux CI or Hetzner-smoke workflow only after secrets/domain handling is designed; keep current Windows Build stable.

## Linux Deploy Debt

Status: single-server dry-run is proven; production hardening is not complete.

Completed:

- Linux-native `libChess3DEngine.so` built on Hetzner;
- Windows vs Linux ABI parity checked;
- `linux-x64` framework-dependent `ChessOnlineServer` package produced;
- package installed under `/opt/chessonline/server`;
- runtime directories exist under `/var/lib/chessonline`, `/var/log/chessonline`, and `/var/backups/chessonline`;
- `chessonline.service` runs loopback Kestrel;
- Nginx proxies public HTTP port 80 to Kestrel;
- public live/ready/diagnostics endpoints pass.

Still stalled/deferred:

- no confirmed domain or DNS record;
- no TLS certificate;
- no HTTPS-only auth/token policy;
- public HTTP remains diagnostic-only;
- no log rotation policy committed as an installed server config;
- no backup/restore rehearsal for `/var/lib/chessonline`;
- no rollback script for `/opt/chessonline/server` package swaps;
- no cleanup/runbook for stale temp VPS build directories.

Recommended next step:

- P4E should focus on domain/TLS, HTTPS auth enforcement, rollback, backups, and operator runbooks before any real user traffic.

## Online Authority Debt

Status: authenticated single-server Asgard smoke works; public multiplayer authority remains MVP.

Completed:

- auth/session/persistence paths exist;
- matchmaking smoke creates an exact-profile Asgard game;
- remote SignalR Asgard action/snapshot/action-log smoke passes through local-forwarded loopback;
- single-server service and Nginx boundary are live.

Still stalled/deferred:

- reconnect/resume semantics need a hard public contract;
- spectator/read-only flows are not hardened;
- no public ranked matchmaking;
- no anti-cheat policy or enforcement;
- no rate limiting/throttling pass;
- no Redis/Azure SignalR/backplane;
- no multi-node authority;
- no public HTTPS SignalR smoke;
- no durable operational metrics/dashboard.

Recommended next step:

- harden single-server reconnect/session durability before scale-out. Do not introduce a backplane until one-server correctness and TLS are solid.

## Chess2D / Portal Debt

Status: orthodox engine is strong; portal interoperability is missing.

Completed:

- ordinary 8x8 legal move generation;
- FEN import/export;
- checkmate/stalemate/draw status;
- search and benchmark coverage;
- portal capability matrix and prototype Lichess/Chess.com clients.

Still stalled/deferred:

- full PGN/SAN import/export;
- UCI-compatible process adapter;
- safe token storage;
- Lichess Board API vs Bot API policy/UI;
- Chess.com remains read-only through PubAPI;
- no match clock/time-control integration;
- no live portal UI flow.

Recommended next step:

- implement PGN/SAN first, then a small UCI adapter, then a token-safe Lichess connector with mocked-stream tests.

## Chess3D Mode Debt

Status: exactly five RuleProfiles remain real; no sixth mode exists.

Real profiles:

1. `classic-six-side-3d-8x8x8-v0.1`
2. `single-side-3d-8x8x8-v0.1`
3. `asgard-convergence-3d-8x8x8-v0.1`
4. `rubik-convergence-3d-8x8x8-v0.1`
5. `hodge-projection-duel-3d-8x8x8-v0.1`

Still stalled/deferred:

- old profile JSON/docs still contain stale `draft` notes for Classic/Six-Side king safety even after P3A;
- Asgard destructive fusion/implosion is intentionally not implemented;
- contested-anchor scoring remains deferred;
- reserve restore into core, restore capture, and rich inventory UI remain deferred;
- Rubik arbitrary-state solving remains future;
- Rubik king-safety after layer rotation remains documented as deferred;
- Hodge remains human/manual in several scenario notes despite later AI/search work; wording should be reconciled.

Recommended next step:

- do a documentation consistency pass before changing rules. Runtime rules should stay untouched unless a profile-specific phase explicitly scopes them.

## Visual / Asset Debt

Status: readable WPF/OBJ path exists; richer visuals are still partial.

Completed:

- canonical OBJ/MTL asset layout;
- fallback readable materials;
- shared model catalog for Chess2D and Chess3D;
- visual diagnostics and click-to-move hardening.

Still stalled/deferred:

- GLB/glTF support is future;
- generated model manifest is descriptor-only;
- screenshot TODO list remains manual;
- full stack/fusion/Rubik/Hodge animation quality remains manual visual QA rather than automated screenshot tests;
- no automated visual regression capture.

Recommended next step:

- keep WPF visual improvements incremental; add screenshot/manual QA automation before adding a new asset format.

## AI / Search Debt

Status: profile-aware shallow Chess3D AI/search exists; strength is not a product-grade engine.

Completed:

- profile-aware candidate generation;
- bounded deterministic search;
- Classic/Single king-safe candidates;
- Rubik layer-turn and Hodge composite candidates;
- summary JSON and UI search integration.

Still stalled/deferred:

- no transposition table;
- no deep tactical model for custom modes;
- no public anti-cheat boundary;
- no portal-safe engine-assistance policy for Chess2D;
- no per-profile strength/quality benchmark beyond current diagnostics.

Recommended next step:

- separate engine-strength work from online gameplay policy. Do not use search in live human portal accounts without a bot/engine-allowed mode.

## Documentation Contradictions / Stale Notes

Status: most docs are honest, but older files now contradict later phases.

Examples found:

- `docs/CHESS3D_RULE_PROFILES.md` still says Classic checkmate is draft;
- `assets/rules/profiles/classic_six_side_3d_v0_1.json` still marks `checkmateImplementation` as `draft`;
- old P4D docs still say Linux-native authority is blocked, while later Next Era phases prove Linux native build/package/service smoke;
- old scenario files say replay/import/export and AI/search are deferred even though later phases implemented parts of those pipelines;
- some architecture text still refers to Windows-native authority as current in sections predating P4D1/Next Era.

Recommended next step:

- run a dedicated documentation reconciliation phase. Do not delete historical docs blindly; add "superseded by" notes or update central docs so operator-facing guidance is unambiguous.

## Deployment / Security Debt

Status: no secrets are tracked; public security posture is not production-complete.

Completed:

- no private keys, tokens, certs, runtime DBs, stores, keyrings, or raw SSH secrets are committed;
- deployment docs use placeholders;
- public HTTP is explicitly diagnostic-only.

Still stalled/deferred:

- confirmed domain/DNS;
- TLS certificate issuance and renewal;
- HTTPS redirect and HSTS decision;
- keyring encryption/protection beyond filesystem permissions;
- log rotation;
- rate limiting;
- backup/restore;
- secret storage for portal tokens;
- operator access model and non-root routine maintenance.

Recommended next step:

- P4E should be security/deployment hardening, not gameplay expansion.

## Priority Order

1. TLS/domain + HTTPS auth enforcement.
2. Deployment rollback, backup, and log rotation.
3. Documentation consistency pass for stale `draft`/`blocked` notes.
4. Chess2D PGN/SAN and UCI adapter.
5. Reconnect/resume and public SignalR smoke over HTTPS.
6. Visual QA automation and screenshot checklist execution.
7. AI/search quality work and anti-cheat policy.
