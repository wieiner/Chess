# P4K Reproducible Server Package

Date: 2026-07-11

## Goal

Phase 04 hardens `scripts/deploy/Publish-ChessOnlineServer-Linux.ps1` so a future Hetzner deploy package can be inspected, hashed, and traced back to a source commit before it is copied to the server.

This phase does not deploy anything and does not touch nginx, UFW, TLS/443, x-ui/Xray, Outline, Albatronix Docker, Unreal, or runtime data.

## Script Parameters

Existing compatible parameters remain:

- `-Configuration`;
- `-Runtime`;
- `-OutputPath`;
- `-NativeLibraryPath`;
- `-CommitSha`;
- `-PackageId`.

New hardening parameters:

- `-ManifestPath`: explicit manifest destination. Defaults to `server-package-manifest.json` in the output directory.
- `-Clean`: removes the selected output directory before publish, but only when the resolved output path is under `.tmp/`.
- `-FailOnSecretLikeFiles`: fails if package output contains secret-like/runtime file names.

## Required Output

The script now verifies:

- `ChessOnlineServer.dll`;
- `ChessOnlineProtocol.dll`;
- `ChessOnlinePersistence.dll`;
- `ChessOnlineServer.runtimeconfig.json`;
- `ChessOnlineServer.deps.json`;
- `appsettings.Production.sample.json`;
- `server-build.json`;
- exactly five real Chess3D RuleProfile JSON files;
- `libChess3DEngine.so` when `-NativeLibraryPath` is supplied.

The script rejects `Chess3DEngine.dll` in a Linux package.

The script also removes local/development-only output before manifest generation:

- `appsettings.Development.json`;
- `appsettings.Local.json`;
- `*.pdb`.

## Build Identity

The script writes:

```text
server-build.json
```

with:

- `commit`;
- `builtUtc`;
- `packageId`;
- `informationalVersion`.

It does not include local paths, usernames, machine names, tokens, passwords, certs, or key material.

## Manifest

The script writes:

```text
server-package-manifest.json
```

with:

- format: `chessonline-server-package-manifest`;
- version: `0.1`;
- package id;
- commit;
- runtime;
- created UTC;
- file count;
- per-file relative path, length, and SHA-256.

The manifest is intended for operator verification and later archive hashing. It is not a runtime store.

## Secret-Like File Guard

With `-FailOnSecretLikeFiles`, the script rejects file names matching sensitive/runtime patterns such as:

- `*.key`;
- `*.pem`;
- `*.pfx`;
- `*.db`;
- `*.sqlite`;
- `key-*.xml`;
- `*password*`;
- `*token*`;
- `*.secret`;
- `*.secrets`;
- `secret.*`;
- `secrets.*`;
- `*keyring*`;
- `known_hosts`;
- `id_ed25519*`;
- `chess3d-online-store.json`.

This is a package guard, not a replacement for later secret scans.

## Verification

Phase 04 verification uses only local `.tmp` output:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\deploy\Publish-ChessOnlineServer-Linux.ps1 `
  -OutputPath .tmp\p4k-phase04-publish `
  -CommitSha testcommit `
  -PackageId p4k-phase04-selfcheck `
  -Clean `
  -FailOnSecretLikeFiles

Get-Content .tmp\p4k-phase04-publish\server-package-manifest.json -Raw | ConvertFrom-Json
```

The `.tmp` output is ignored and must not be committed.
