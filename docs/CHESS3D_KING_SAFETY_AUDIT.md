# Chess3D King Safety Audit

P2O does not attempt a full six-side checkmate rewrite.

## Current Encoding

Piece codes are `side * 10 + pieceType`; king type is `6`. Board cells are 512 projected integer cells, with CoreCell stack overlay only for stack-enabled profiles.

## Attack Generation

The engine has 3D pseudo-legal movement generation for pawn, knight, bishop/officer, rook, queen, and king. P2O derives draft check status by finding a side king on the projected board and asking whether any opposing generated move targets that square.

## Legal vs Pseudo-Legal

Classic move generation is still not fully king-safe. Legal preview and `TryMakeMove` are reliable for movement/capture contracts, but they do not yet filter every move that leaves a king in check.

## P2O Runtime Status

- `Chess3D_IsSideInCheck` exposes draft check status for Classic-style checkmate profiles.
- `Chess3D_GetSideLegalActionCount` exposes current pseudo-legal/profile-aware action count.
- `Chess3D_GetCheckStatusSummary` marks the status as `draft` or `notApplicable`.

## Profile Scope

- Classic Six-Side: check/stalemate/checkmate are draft diagnostics.
- Single-Side: checkmate not applicable.
- Asgard/Rubik: centerAssembly is the active outcome; king safety deferred.
- Hodge: macro-player outcome is deferred.

Full 3D king safety/check/mate/stalemate belongs to P3A.
