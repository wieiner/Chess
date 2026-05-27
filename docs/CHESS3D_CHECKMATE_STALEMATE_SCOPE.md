# Chess3D Checkmate And Stalemate Scope

P3A applies real checkmate/stalemate only to Classic Six-Side and the king-bearing parts of Single-Side Training.

## Classic Six-Side

Classic now uses legal actions, not pseudo-actions, when deciding checkmate and stalemate. The side to move is the tested side.

Checkmate:

- current side has a king;
- current side is in check;
- current side has zero legal actions.

Stalemate:

- current side has a king;
- current side is not in check;
- current side has zero legal actions.

Winner side for checkmate follows the current six-side turn convention: the previous side is reported as winner. Stalemate has no winner.

## Single-Side Training

Single-Side keeps its training identity, but legal preview and moves use the same king-safety kernel when a king exists. Missing-king training positions remain constructible.

## Not Applied

Asgard, Rubik, and Hodge do not use Classic checkmate as their game outcome unless a future profile explicitly opts into that contract.
