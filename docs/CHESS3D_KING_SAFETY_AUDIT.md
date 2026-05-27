# Chess3D King Safety Audit

P2O did not attempt a full six-side checkmate rewrite. P3A closes the Classic/Single-Side king-safety layer while leaving Asgard/Rubik/Hodge outcome rules isolated.

## Current Encoding

Piece codes are `side * 10 + pieceType`; king type is `6`. Board cells are 512 projected integer cells, with CoreCell stack overlay only for stack-enabled profiles.

## Attack Generation

The engine has 3D pseudo-legal movement generation for pawn, knight, bishop/officer, rook, queen, and king. P2O derived draft check status by finding a side king on the projected board and asking whether any opposing generated move targets that square; P3A replaces that with the runtime attack kernel described in `CHESS3D_KING_SAFETY_RUNTIME.md`.

## Legal vs Pseudo-Legal

The pseudo-legal generator remains intact. P3A adds a legal filter for Classic/Single-Side consumers: legal preview, `TryMakeMove`, side legal-action counts, outcome checks, perft, and divide.

## P3A Runtime Status

- `Chess3D_IsSideInCheck` exposes runtime check truth for Classic/Single-Side when a king is present.
- `Chess3D_GetSideLegalActionCount` uses king-safe legal actions for Classic/Single-Side.
- `Chess3D_GetCheckStatusSummary` marks Classic/Single-Side status as `runtime`.

## Profile Scope

- Classic Six-Side: check/stalemate/checkmate are runtime outcomes.
- Single-Side: king-safety filtering applies when a king is present; it remains a training profile.
- Asgard/Rubik: centerAssembly is the active outcome; king safety deferred.
- Hodge: macro-player outcome is deferred.

AI/search and richer UI animation remain later work.
