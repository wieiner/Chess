# Next Era Chess2D Portal Integration Audit

Date: 2026-06-21

Scope: audit only. This phase does not implement portal integration, does not store tokens, does not submit moves to external services, and does not change Chess2D or Chess3D rules.

## Executive Summary

The repository already has a solid ordinary 8x8 Chess2D rules core:

- legal move generation with self-check filtering;
- check, checkmate, stalemate, draw-claim, and automatic draw status;
- FEN import/export;
- castling, en passant, promotion, and move flags;
- native search and benchmark coverage;
- WPF board/advisor UI that uses legal moves and FEN.

The missing pieces for external chess portals are not basic move legality. They are interchange and protocol layers:

- full PGN/SAN import/export;
- UCI-compatible engine process adapter if the engine should plug into ordinary chess GUIs;
- safe account/token storage and a clear human-vs-bot policy for Lichess;
- a read-only Chess.com path unless an approved interactive API is available;
- time-control and clock semantics beyond search time limits;
- production UI flows for portal challenge/game streams.

Chess3D modes remain outside public orthodox chess portals. Classic, Single-Side, Asgard, Rubik, and Hodge are custom Chess3D RuleProfiles and should use the project's own online server, not Lichess/Chess.com gameplay APIs.

## Local Chess2D Runtime

| Area | Current status | Evidence |
| --- | --- | --- |
| Board/rules | Orthodox 8x8 board with signed piece codes and side-to-move state. | `src/ChessEngine/ChessEngine.h`, `src/ChessEngine/ChessEngine.cpp` |
| Legal move generation | Implemented through pseudo-move generation plus legal filtering around king safety. | `generatePseudoMoves`, `generateLegalMoves`, `isInCheck` in `ChessEngine.cpp` |
| Special moves | Castling, en passant, and promotion are represented in move generation and flags. | `ChessMoveDto` flags and contract tests |
| Game status | Playing, checkmate, stalemate, repetition claim/draw, fifty-move claim, and seventy-five-move draw are exposed. | `ChessStateDto` and `Chess_GetState` |
| FEN | Import/export exists through `Chess_SetFen` and `Chess_GetFen`; UI exposes load/copy FEN. | `NativeChessEngine.cs`, `MainWindow.xaml.cs`, contract tests |
| PGN/SAN | Not implemented as a full standard export/import layer. UI move history uses compact coordinate notation. | `MainWindow.xaml.cs` `FormatMove`; no full PGN parser found |
| UCI | Not implemented as a process protocol. The native ABI exposes direct engine calls instead. | No UCI command loop found |
| Search | Native search exists with depth, time-limit, ordering, quiescence option, and telemetry. | `ChessSearchOptionsDto`, `Chess_MakeBestMoveEx`, UI search controls |
| Clock/time control | Search has a time limit, but there is no complete match clock/time-control model. | `TimeLimitBox` is search-oriented |
| Benchmark | Native `Chess2DBenchmark` measures legal move generation, search, and evaluation corpus paths. | `src/Chess2DBenchmark/Chess2DBenchmark.cpp` |

## Existing Portal-Oriented Code

`src/ChessOnlineApp/Integrations/OnlineChessPortals.cs` already defines a capability matrix for ordinary chess portals and the custom ChessAdvisor 3D web platform.

Important current boundaries:

- Lichess is modeled as an official HTTPS NDJSON Board API / Bot API target with bearer-token auth.
- Chess.com is modeled as public/archive/current-game data only. Its Published Data API is read-only.
- ICS-style servers have a line-oriented `IcsTextChessClient` foundation.
- The custom 3D web platform is explicitly separate from ordinary chess portals.

`src/ChessOnlineApp/Integrations/LichessClient.cs` is already a useful prototype client. It can:

- read account JSON;
- stream incoming events;
- stream Board API or Bot API game events;
- submit moves through Board API or Bot API endpoints;
- write chat;
- create challenges.

This code should still be treated as a prototype until token storage, account-mode policy, rate-limit behavior, and UI consent flows are hardened.

## Official Portal Constraints

| Portal/protocol | Relevant finding | Repo implication |
| --- | --- | --- |
| Lichess API | Lichess exposes official APIs and updates endpoint documentation at `lichess.org/api`. | Best first live-play target for Chess2D, but token handling and human/bot separation must be explicit. |
| Lichess Board API | Board API is for playing with physical boards and third-party clients, not engine-assisted human cheating. | Chess Advisor must never use Board API to provide illegal engine assistance on a normal human account. |
| Lichess Bot API | Bot/engine play belongs in bot workflows. | If this project automates engine moves, it should use a bot-account path and follow Lichess rules. |
| Chess.com PubAPI | The published-data API is read-only and cannot submit moves or commands. | Use Chess.com for profile/archive/current-game imports only unless an approved interactive API exists. |
| PGN/FEN | PGN is the portable game-record format; FEN is the position snapshot format. | PGN/SAN must be added before robust import/export, replay, or portal archive workflows. |
| UCI | UCI is the common text protocol for engine/GUI communication. | A UCI adapter should be a separate executable/process boundary, not a replacement for the native ABI. |

## What Is Already Orthodox Enough

The 2D engine appears suitable as a base for orthodox chess interoperability because contract tests cover:

- initial position legal count of 20;
- normal legal move acceptance;
- blocked move rejection;
- FEN roundtrip;
- castling FEN;
- en passant;
- promotion;
- checkmate and stalemate status;
- draw-rule exposure;
- search statistics.

Before portal move submission, add a few portal-specific regression positions:

- SAN disambiguation and check/checkmate suffix;
- legal move comparison from FEN snapshots received from a portal;
- promotion notation compatibility;
- castling notation compatibility;
- game termination/result mapping.

## Missing Production Pieces

| Gap | Risk | Suggested phase |
| --- | --- | --- |
| Full PGN/SAN parser/generator | Cannot reliably import/export games or match portal notation. | Phase A |
| UCI process adapter | Cannot plug the engine into ordinary UCI GUIs/tools. | Phase B |
| Portal token storage | Secrets could be mishandled if stored in app JSON or logs. | Phase C precondition |
| Lichess Board/Bot policy | Risk of engine assistance on human accounts. | Phase C precondition |
| Chess.com interactive play | Current official public API is read-only. | Do not implement unless a supported API exists |
| Match clock/time control | Portal games need clocks, increments, disconnect policies, and result handling. | Phase C/D |
| Resilient NDJSON stream lifecycle | Live APIs need reconnect, backoff, idempotence, and cancellation. | Phase C |
| Portal UI consent/errors | Users need clear "read-only", "bot", and "human board" mode distinctions. | Phase C |

## Recommended Integration Plan

### Phase A - FEN/PGN Export/Import

Implement ordinary chess interchange first:

- keep existing `Chess_SetFen` and `Chess_GetFen`;
- add PGN export with standard tag pairs and SAN move text;
- add PGN import into engine move sequences;
- add validation tests using known PGN/FEN fixtures;
- keep Chess3D replay/action notation separate from PGN.

### Phase B - UCI-Compatible Engine Adapter

Add a small console executable, not a broad engine rewrite:

- command loop for `uci`, `isready`, `position`, `go`, `stop`, `quit`;
- map `position fen` and `position startpos moves ...` to the native engine;
- map `go depth` and `go movetime` to existing search options;
- output `bestmove` in long algebraic notation;
- document unsupported UCI options honestly.

### Phase C - Lichess Connector

Use the existing `LichessClient` as a foundation, but harden it before live play:

- store secrets outside repo files, preferably OS credential storage;
- separate Board API human-client mode from Bot API engine-play mode;
- use explicit user consent and warnings for engine-assistance boundaries;
- add cancellation/timeouts for NDJSON streams;
- map game snapshots to local FEN and legal moves;
- map local moves to Lichess move strings;
- add test doubles for stream and move-submit flows.

### Phase D - Portal Boundaries For Chess3D

Do not try to run custom Chess3D modes on public orthodox portals:

- Classic Six-Side, Single-Side, Asgard, Rubik, and Hodge are not ordinary 8x8 2D chess games;
- public orthodox portals cannot represent 8x8x8 boards, reserve/stack/fusion actions, Rubik turns, or Hodge projected composite moves;
- Chess3D online play belongs on the project's own ChessOnlineServer authority;
- ordinary portals may still be used for user profile/archive inspiration and 2D games.

## Safety Rules

- Do not commit bearer tokens, cookies, passwords, account stores with real secrets, or portal session data.
- Do not automate moves on a human account where a portal forbids engine assistance.
- Do not scrape browser UIs when an official API does not permit gameplay commands.
- Do not claim Chess.com move submission support while only PubAPI is available.
- Do not route Chess3D RuleProfiles through PGN or UCI except as documentation/export summaries.

## Suggested Test Gates

Before any live portal integration:

1. Chess2D contract tests remain green.
2. PGN import/export roundtrip tests pass.
3. UCI adapter self-tests pass without network.
4. Lichess connector tests run against mocked HTTP/NDJSON streams.
5. Manual live Lichess smoke uses a disposable/test account mode and never logs secrets.
6. Chess.com connector remains read-only unless an official interactive API is confirmed.
