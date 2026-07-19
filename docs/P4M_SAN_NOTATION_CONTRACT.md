# P4M SAN Notation Contract

## Scope

This contract defines canonical Standard Algebraic Notation for the orthodox Chess2D engine. It is locale-independent and generated from authoritative legal context. Chess3D profile notation, Rubik notation, UI labels, comments, NAGs, variations, and PGN result markers are separate concerns.

Primary reference: Steven J. Edwards, [Portable Game Notation Specification and Implementation Guide](https://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm), section 8.2.3 and related import/export rules.

## Inputs

SAN generation requires:

- normalized pre-move position;
- one exact legal move, including promotion choice;
- all legal moves in the pre-move position for disambiguation;
- moved and captured piece identity before mutation;
- resulting position status after a temporary or committed application.

The generator must reject an input move that is not in the legal move set. It must not infer legality from coordinates or DTO flags alone.

## Piece designators

Canonical English uppercase letters are used:

| Piece | SAN letter |
|---|---|
| King | `K` |
| Queen | `Q` |
| Rook | `R` |
| Bishop | `B` |
| Knight | `N` |
| Pawn | none |

Unicode glyphs and localized names are presentation only and never SAN authority.

## Destination and capture

Every non-castling SAN move contains the lower-case destination square (`a1` through `h8`).

- A non-capture has no separator: `e4`, `Nf3`, `Rae1`.
- A capture inserts `x` immediately before the destination: `Nxe5`, `R1xa3`.
- A pawn capture always includes its source file: `exd5`.
- En passant uses ordinary pawn-capture SAN, for example `exd6`. Canonical export does not append `e.p.`; a future importer may accept that text as a permissive annotation.

Capture truth is derived before applying the move. For en passant, the destination is empty but the move is still a capture.

## Disambiguation

Disambiguation applies to non-pawn, non-castling moves when another piece of the same side and type has a legal move to the same destination. Pseudo-legal moves from pinned or otherwise illegal pieces do not create ambiguity.

Let `conflicts` be the other legal moves with the same piece type, side, and destination:

1. If `conflicts` is empty, append no origin coordinate.
2. If no conflict starts on the moving piece's file, append the source file: `Nbd2`.
3. Otherwise, if no conflict starts on the moving piece's rank, append the source rank: `R1e2`.
4. Otherwise append both source file and rank: `Qh4e1`.

This algorithm handles two or more candidate pieces deterministically. Captures use the same origin qualifier before `x`, for example `R1xa3`.

Kings normally need no disambiguation because a legal position has one king per side. Invalid positions must not be used to manufacture noncanonical king SAN.

## Castling

- King-side castling: `O-O`.
- Queen-side castling: `O-O-O`.

Canonical output uses uppercase letter `O`, not zero. Castling does not include king destination or a capture marker. Check or checkmate suffixes are appended normally (`O-O+`, `O-O-O#`).

## Promotion

Promotion appends `=` and the promoted piece letter:

- `e8=Q`;
- `fxg8=N+`.

Only `Q`, `R`, `B`, and `N` are valid promotion letters. The promoted piece is part of the move identity; omitted promotion may default to queen at the engine invocation boundary, but a committed record must store the resolved choice explicitly.

## Check and checkmate

Suffixes are determined from the post-move position:

- `+` when the opposing king is in check and the opponent has at least one legal move;
- `#` when the opposing king is in check and the opponent has zero legal moves;
- no suffix for stalemate or an ordinary move.

Canonical output uses one `+` for check and `#` for checkmate. It does not emit `++`, `mate`, or localized text. Discovered and double check still use the same single `+` unless they are mate.

The current `ChessMoveDto` check flag is useful but insufficient to distinguish check from mate. Generation must inspect the resulting `ChessStateDto` or equivalent legal context.

## Construction order

For a legal non-castling move:

```text
piece-letter
+ disambiguation (or source file for pawn capture)
+ capture marker when applicable
+ destination square
+ promotion when applicable
+ check/checkmate suffix
```

Examples:

| Situation | SAN |
|---|---|
| Pawn move | `e4` |
| Piece move | `Nf3` |
| Pawn capture | `exd5` |
| Piece capture | `Qxh7+` |
| File disambiguation | `Nbd2` |
| Rank disambiguation | `R1e2` |
| File and rank disambiguation | `Qh4e1` |
| En passant | `exd6` |
| Promotion | `e8=Q` |
| Capture promotion mate | `fxg8=Q#` |
| King-side castle | `O-O` |
| Queen-side castle with check | `O-O-O+` |

## SAN versus surrounding PGN tokens

The following are not part of a SAN token:

- move numbers (`1.`, `1...`);
- game termination markers (`1-0`, `0-1`, `1/2-1/2`, `*`);
- comments (`{...}` or semicolon comments);
- NAGs (`$1`) and punctuation annotations (`!`, `?!`);
- recursive annotation variation delimiters;
- clock/evaluation annotations.

The PGN exporter composes these around canonical SAN. `ChessMoveRecord.San` stores only the SAN token.

## UCI distinction

Coordinate UCI notation (`e2e4`, `e7e8q`) is a deterministic move identifier but is not SAN. SAN can be generated only with position context because capture, ambiguity, castling, and check/mate depend on that context.

## Import tolerance and export strictness

P4M export emits only the canonical forms above. A later parser may accept common import variations such as `0-0`, `0-0-0`, trailing `e.p.`, or redundant check annotations, but it must normalize the resolved legal move back to canonical SAN. Tolerance must never allow an illegal or ambiguous move to resolve silently.

## Error contract

SAN generation fails without mutating engine or history when:

- coordinates are outside the board;
- the source is empty or belongs to the wrong side;
- the requested promotion is invalid;
- no exact legal move matches;
- more than one legal promotion candidate remains unresolved;
- post-move status cannot be obtained transactionally;
- position state is internally invalid.

Failure returns a structured reason; it must not return a plausible-looking fallback string.

## Required fixtures

The implementation gate covers at least:

- ordinary pawn and piece moves;
- ordinary and en-passant captures;
- file, rank, and both-coordinate ambiguity;
- pinned pseudo-candidate excluded from ambiguity;
- both castlings;
- all four promotion pieces, with and without capture;
- direct, discovered, and double check;
- checkmate and stalemate distinction;
- illegal move no output and no mutation;
- deterministic repeat generation.
