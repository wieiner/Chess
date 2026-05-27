# Chess3D Classic Checkmate And Stalemate

This document mirrors `CHESS3D_CHECKMATE_STALEMATE_SCOPE.md` for the older P3A documentation name.

Classic Six-Side now has engine-backed king safety:

- legal moves cannot leave the moving side's king attacked;
- kings cannot move into attacked cells;
- line checks can be blocked when the existing 3D movement model supports the blocker move;
- checking pieces can be captured when the resulting position is safe;
- checkmate and stalemate are real `GameOutcome` states for Classic.

Single-Side Training uses the same legal filter when a king is present, while Asgard/Rubik/Hodge remain isolated from Classic checkmate semantics.
