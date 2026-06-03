# Chess3D Profile-Aware Search

The P3D search layer uses existing profile semantics rather than adding a generic chess-only move generator. P3D.1 hardens that layer with iterative deepening, alpha-beta discipline, deterministic ordering, bounded quiescence-lite, and summary JSON v2.

## Classic / Single-Side

Candidates are king-safe legal actions. Self-check and king-into-check moves are absent from the candidate list.

## Asgard

Candidates include legal moves/captures and reserve restore candidates when reserve is enabled and a matching home slot is free. Core stacks, fusion, anchors, and reserve counts remain isolated through copied-state search.

## Rubik

Rubik candidates include Asgard-style actions plus legal ritual layer turns. Layer turns are generated only when the active profile enables `layerTurnProfile.type = ritualTurn`.

## Hodge

Hodge candidates are all-or-nothing projected composite moves for the current macro-player. The search treats the primary and two mirror moves as one action.

## Isolation

Asgard, Rubik, and Hodge do not inherit Classic checkmate as a victory condition. Classic and Single-Side do not inherit Asgard core/fusion, Rubik layer turns, or Hodge projection.

## P3D.1 Guarantees

Candidate generation and search remain non-mutating. Only explicit `ApplyAiAction` and `MakeBestProfileAction` calls commit through existing legal runtime paths. There is still no external engine, opening book, GPU search, online authority, or transposition-table implementation.
