# P4J Phase 11 - Spectator Contract Audit

Date: 2026-07-01

## Current State

There is no implemented spectator mode yet.

Current online table access is seat-oriented:

- `JoinTableSeat` allocates a player seat;
- `Ready`, `StartGame`, and `SubmitAction` require the caller to be seated;
- `RequestSnapshot` and `RequestActionLog` currently go through the same authenticated hub/registry path and are intended for active table participants;
- table broadcasts use SignalR groups such as the table group.

This is a good base for spectator mode because read-only viewers can join the table group without becoming seats.

## Recommended P4J Spectator Contract

Spectator mode should be read-only and authenticated.

For P4J:

- spectator should require a temporary authenticated user;
- spectator should not allocate or occupy a player seat;
- spectator may join a table group for broadcasts;
- spectator may request an authoritative snapshot;
- spectator may request action-log chunks;
- spectator may receive accepted action events and authoritative snapshot/resync events;
- spectator may save sanitized spectator reports.

Anonymous public spectating is deferred. Public unauthenticated spectators would create a different privacy and abuse boundary and should be considered only after production auth/TLS decisions.

## Mutations Forbidden For Spectators

Spectator identity must not be allowed to:

- `Ready`;
- `StartGame`;
- `SubmitAction`;
- claim a seat implicitly;
- bypass turn/seat authority;
- use legal preview as permission to mutate state.

If the UI offers legal preview while spectating, it must be labelled inspection-only and generic submit must remain disabled.

## Snapshot And Action Log

Spectator join should return:

- room id;
- table id;
- ruleset id;
- spectator/viewer id;
- current authoritative snapshot when available;
- action-log tail from an optional `lastKnownServerSeq`;
- clear failure reason on invalid room/table/profile.

The result must not include access tokens, refresh tokens, temporary passwords, private key material, raw connection ids, or runtime store paths.

## Server Method Shape

Proposed append-only hub method:

- `JoinSpectator`

Proposed messages:

- `JoinSpectator`
- `JoinSpectatorResult`

The server should validate:

1. caller is authenticated;
2. room/table exists;
3. optional expected ruleset matches;
4. table has an active runtime session if snapshot/action log is requested.

On success:

- add the connection to the table SignalR group;
- record connection membership as read-only spectator if needed;
- do not persist a seat assignment;
- return snapshot/action log.

## Client And UI Boundary

Client support should include:

- `JoinSpectatorAsync`;
- `LastSpectatorResult`;
- a read-only spectator state;
- submit-disabled policy with a readable reason.

UI support should include:

- spectator mode badge;
- room/table id entry or later lobby selection;
- `Join as Spectator`;
- `Request Snapshot`;
- `Request Action Log`;
- disabled `Ready`, `Start`, and submit controls.

## Privacy

Do not expose:

- access/refresh tokens;
- passwords;
- authorization headers;
- raw connection ids;
- full private player details;
- persistent store/keyring paths.

It is acceptable to expose:

- room id;
- table id;
- ruleset id;
- table state;
- seat occupancy count;
- action notation;
- state hash;
- server sequence numbers.

## Deferred

- anonymous public spectator mode;
- spectator chat;
- spectator permissions/ban lists;
- production-grade privacy controls;
- server restart spectator resume.

HTTP 80 remains diagnostic/dev only, with temporary users only.
