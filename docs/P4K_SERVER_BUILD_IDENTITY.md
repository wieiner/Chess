# P4K Server Build Identity

Date: 2026-07-11

## Goal

After a Hetzner deploy, an operator must be able to ask `/chess3d/diagnostics` which repository/package build is running. Before P4K Phase 02, diagnostics exposed a top-level `serverCommit` field, but the running server did not have a reliable package metadata file.

## Runtime Contract

`/chess3d/diagnostics` now includes an append-only `build` object:

```json
{
  "serverCommit": "<commit-or-empty>",
  "build": {
    "commit": "<commit-or-empty>",
    "builtUtc": "<utc-or-empty>",
    "packageId": "<package-id-or-empty>",
    "informationalVersion": "<assembly-version-fallback>"
  }
}
```

Existing diagnostics fields remain unchanged. Existing clients that ignore unknown JSON fields remain compatible.

## Metadata Source

The server reads:

```text
server-build.json
```

from `AppContext.BaseDirectory`, next to `ChessOnlineServer.dll`.

Expected file shape:

```json
{
  "commit": "full-git-sha",
  "builtUtc": "2026-07-11T00:00:00.0000000Z",
  "packageId": "chessonline-linux-x64-<short-sha>",
  "informationalVersion": ""
}
```

If the file is absent, malformed, unreadable, or missing optional fields, server startup still succeeds. `informationalVersion` falls back to the entry assembly informational version.

## Publish Script Behavior

`scripts/deploy/Publish-ChessOnlineServer-Linux.ps1` now accepts:

- `-CommitSha`;
- `-PackageId`.

If `-CommitSha` is omitted, the script best-effort reads `git rev-parse HEAD`. If `-PackageId` is omitted, the script derives:

```text
chessonline-linux-x64-<short-sha>
```

The file is generated in the publish output and is safe to package with the server payload.

## Security Boundary

`server-build.json` must not include:

- local paths;
- usernames;
- machine names;
- tokens;
- passwords;
- SSH key paths;
- certificates;
- runtime store/keyring paths.

The server-side reader trims long fields and rejects path-like commit/package identifiers.

## Verification

Phase 02 checks:

- `dotnet build src\ChessOnlineServer\ChessOnlineServer.csproj -c Release`;
- `tests\run-tests.ps1 -Only ChessOnlineContractTests -SkipBenchmark -MSBuildMaxCpuCount 1 -GlobalTimeoutSeconds 300`;
- `git diff --check`.

Contract coverage verifies that build identity DTO data roundtrips and does not contain token/path markers.
