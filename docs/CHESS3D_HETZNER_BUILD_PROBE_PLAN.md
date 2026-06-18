# Chess3D Hetzner Build Probe Plan

Status: P4C Phase 14 planning-only. No SSH probe or deployment has been performed in this phase.

## Purpose

This plan defines how to probe a future Hetzner Linux host safely after the Linux-native authority blocker is removed. It keeps infrastructure checks separate from gameplay changes and avoids committing operational secrets.

## Prerequisites

Do not run a real Hetzner probe until all prerequisites are true:

- local source tree is clean;
- `tests/run-tests.ps1 -SkipBenchmark` passes;
- `scripts/verify.ps1` passes;
- GitHub Actions is green;
- a Linux-native Chess3D rules authority artifact exists;
- Linux state-hash parity is proven for all five RuleProfiles;
- no private keys, tokens, stores, keyrings, certs, database files, or real server IPs are added to tracked files.

## Probe Shape

Use placeholders in docs and scripts:

- host: `<HETZNER_HOST>`;
- user: `<DEPLOY_USER>`;
- app root: `/opt/chessonline/server`;
- local health endpoint: `http://127.0.0.1:5077/health`;
- reverse proxy endpoint: future TLS hostname, not committed.

The first probe should be read-only or minimally invasive:

1. Confirm OS, architecture, and free disk.
2. Confirm installed .NET runtime/SDK if required.
3. Confirm systemd and nginx availability.
4. Upload a disposable build to a temporary path only after Linux package readiness exists.
5. Run health and diagnostics checks.
6. Remove temporary artifacts if the probe fails.

## What Must Not Happen

- No production credentials in the repository.
- No committed real IP address.
- No persistent store/keyring copied back into Git.
- No broad firewall opening in tracked scripts.
- No public ranked matchmaking claim.
- No declaration that Hetzner is production-ready before the authority artifact and parity tests exist.

## Expected P4D Output

The future P4D phase should produce:

- Linux native authority artifact;
- Linux server package or verified hosted path;
- platform-aware native loading docs/tests;
- state-hash parity report;
- explicit go/no-go for a Hetzner runtime smoke.
