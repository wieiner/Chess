# Chess3D Online Rules Authority Adapter

Status: P4C Phase 03 runtime boundary.

## Purpose

The hosted SignalR server needs to become portable-ready without changing Chess3D gameplay rules. P4C Phase 03 introduces a small rules authority adapter boundary around the current Windows-native Chess3D engine.

This does not make Linux runtime complete. It makes the blocker explicit and localizes the future replacement point.

## Contracts

`IChessOnlineRulesAuthority` owns one authoritative game session:

- ruleset id;
- state hash;
- action count;
- game phase/outcome;
- turn summary;
- first legal normal move helper;
- AI candidate helper;
- action application;
- snapshot creation.

`IChessOnlineGameSessionFactory` creates authorities and reports runtime diagnostics.

`OnlineAuthorityRuntimeDiagnostics` reports:

- `RuntimeKind`;
- platform and OS;
- process architecture;
- native library name/path;
- whether this is a portable runtime;
- whether the current runtime is supported.

## Current Implementation

`OnlineGameSession` now implements `IChessOnlineRulesAuthority`.

`NativeChessOnlineGameSessionFactory` creates `OnlineGameSession` and reports the current implementation as `WindowsNative` when running on Windows with `Chess3DEngine.dll` available.

`OnlineRoomRegistry` now depends on `IChessOnlineGameSessionFactory` instead of constructing sessions directly. Existing room/table/action behavior remains unchanged.

`ChessOnlineServer` diagnostics now include authority runtime fields so operators can see that the current server uses the Windows-native authority.

## What Changed

Before P4C Phase 03:

- `OnlineRoomRegistry` directly constructed `OnlineGameSession`.
- `OnlineTable.Session` was typed as `OnlineGameSession`.
- the server diagnostics did not expose the rules authority runtime kind.

After P4C Phase 03:

- `OnlineRoomRegistry` accepts an authority factory;
- `OnlineTable.Session` is typed as `IChessOnlineRulesAuthority`;
- the default factory still uses the existing Windows-native engine;
- diagnostics expose `authorityRuntimeKind`, `authorityIsPortableRuntime`, native library name/path, platform, and architecture.

## Linux Implication

Linux remains blocked until a Linux-compatible authority implementation exists. A future P4D can add one of:

- `LinuxNativeChessOnlineGameSessionFactory` backed by a Linux `Chess3DEngine` shared library;
- a managed authority implementation, if full parity is proven;
- a gateway authority implementation, if Linux transport delegates to a Windows authority process.

Any future implementation must pass state-hash and action replay parity tests for all five real Chess3D RuleProfiles.

## Non-Goals

P4C Phase 03 does not:

- add a sixth RuleProfile;
- change Chess3D rules;
- change online protocol DTO layout;
- remove the Windows-native authority;
- claim Hetzner/Linux runtime readiness;
- add Redis, Azure SignalR, Docker, or public matchmaking.

## Tests

Contract tests verify that the current authority runtime reports `WindowsNative`, remains non-portable, and still exposes `Chess3DEngine.dll` as the native dependency.

Existing online tests continue to validate:

- Classic authority action acceptance;
- Rubik layer action acceptance under the Rubik profile;
- Hodge composite action all-or-nothing behavior;
- Asgard online smoke;
- auth/session/persistence/matchmaking behavior.
