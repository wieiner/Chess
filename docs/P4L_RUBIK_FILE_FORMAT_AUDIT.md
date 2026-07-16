# P4L Rubik File Format Audit

## Scope

P4L needs three separate persistence contracts because physical cube state,
move notation, and application recovery have different trust and compatibility
requirements. This document defines those boundaries before schemas, native
serialization, or WPF file dialogs are implemented.

It applies only to the standalone mechanical `RubikApp`. It does not describe
the Chess3D Rubik Convergence profile and does not change any Chess3D format,
rule profile, native ABI, or online protocol.

## Current implementation

`RubikApp` currently exposes four text-box operations:

- `Export State` formats `Rubik_GetCells` as `N^3` integer cubie IDs;
- `Load State` parses whitespace-separated integers and calls
  `Rubik_SetCells` when the count equals `N^3`;
- `Export Notation` formats the current trusted history as internal
  axis/layer tokens such as `X8`, `Y1'`, and `Z4x2`;
- notation input parses face, wide, whole-cube, and internal coordinate tokens
  and applies the resulting engine moves.

The integer text surface is useful for debugging the current permutation
engine, but it is not a portable physical cube format:

- it has no format identifier or version;
- it has no facelets, sticker orientation, or color scheme;
- it does not validate that cubie IDs form a permutation;
- it cannot represent a photographed or manually entered cube faithfully;
- it is held only in a UI text box, not written transactionally to disk;
- loading calls `Rubik_SetCells`, clears history, and sets `manualState`;
- reverse-history solving therefore correctly refuses the loaded state.

This legacy integer form remains a debug/import aid during migration. It must
not be relabeled as `.rubik.json`.

## Format boundary

| Extension | Authority | Intended use | Must not be used as |
| --- | --- | --- | --- |
| `.rubik.json` | Six physical facelet arrays plus color scheme | Portable cube state, physical editor import/export, solver input | Proof that an attached move history is trusted |
| `.rubikmoves` | Versioned move sequence and notation convention | Scrambles, algorithms, solutions, playback | Complete cube state or application recovery |
| `.rubiksession.json` | Cube state plus verified provenance and local UI/session data | Autosave, crash recovery, recent local work | Interchange format required by third-party solvers |

The three extensions are intentionally not aliases. UI commands must label
them clearly and use a filter specific to the selected operation.

## `.rubik.json`: portable state

The future version 1 root is a UTF-8 JSON object with:

- `format: "rubik.state"`;
- `version: 1`;
- `size` in the engine-supported range;
- canonical `colorScheme` entries for `U,R,F,D,L,B`;
- exactly six face arrays in canonical `U,R,F,D,L,B` order;
- exactly `N*N` valid color values per face;
- optional `history` as untrusted provenance;
- `source`, `stateHash`, and `createdUtc`;
- optional `metadata` extension data.

Face coordinates and order come from
`P4L_RUBIK_FACELET_COORDINATES.md`. Facelets are the only state authority.
Cubie IDs and discrete orientations are reconstructed and validated rather
than persisted as a second competing truth.

An optional history does not make a state eligible for
`SolveByReverseHistory`. A loader may mark history trusted only after replaying
it from a declared initial state and proving that the resulting canonical
facelet hash equals `stateHash`. Otherwise it remains display-only provenance.

## `.rubikmoves`: move sequence

Version 1 is UTF-8 text so algorithms remain readable and diffable. Its first
non-blank line is a required header:

```text
# rubik.moves version=1 notation=wca-v1 size=11
```

Subsequent lines contain canonical WCA-style tokens. `#` comments are allowed
as metadata and phase labels. Required header fields are strict:

- `version` selects grammar and compatibility behavior;
- `notation` distinguishes canonical `wca-v1` from the current internal
  `axis-v1` coordinate notation;
- `size` bounds layer/wide-turn tokens.

Solution files may use the suffix `.solution.rubikmoves` without changing the
format. A move file never implies a starting position; replay requires the
caller to select or verify one explicitly. Import parses the entire sequence
before applying any move.

Legacy notation pasted into the text box remains available as an explicit
legacy path. It is not silently assigned `wca-v1`, because the current X/Z
face-token signs differ from the canonical convention documented in Phase 02.

## `.rubiksession.json`: local session

The future session root uses:

- `format: "rubik.session"`;
- `version: 1`;
- an embedded versioned `rubik.state` document;
- trusted history records in an explicit notation/axis convention;
- verification hashes for the history start and end states;
- current solution/playback position;
- camera, selected axis/layer, surface-only mode, and editor draft;
- dirty/autosave/recovery metadata;
- optional `metadata` extension data.

A session may restore local convenience state, but loading it still validates
the embedded physical state first. Camera or editor metadata cannot authorize
an invalid cube. Session autosaves belong under user-local application data,
not in the repository or beside an explicit user file by default.

## Version and compatibility policy

1. `format` is required and must match the selected loader.
2. Version is a positive integer. Version 1 readers accept only major version
   1; no best-effort reinterpretation of later major versions is allowed.
3. Additive optional data belongs under `metadata`. Readers preserve that
   object as JSON extension data when loading and re-saving.
4. Unknown root members are rejected in strict validation. This catches
   misspelled required properties instead of silently dropping them.
5. A future format may advertise optional capabilities inside `metadata`, but
   unknown required semantics must use a future major version.
6. Writers emit canonical face order and stable property naming. Property
   order is not semantically significant, except for canonical state-hash
   serialization defined by the later schema/serializer phase.
7. No format stores absolute source paths. Optional display names are plain
   labels, not filesystem authority.
8. Files are UTF-8 without a byte-order mark. Readers may accept a UTF-8 BOM
   for compatibility but writers do not emit one.

## Transactional load

All imports follow parse, validate, commit:

1. read into a bounded buffer;
2. parse into a temporary document;
3. validate format, version, size, colors, counts, coordinates, hashes, and
   physical invariants available at that stage;
4. construct a temporary engine state;
5. commit only after every required check succeeds;
6. refresh history/UI only after the engine commit succeeds.

Any failure leaves cube cells, facelets, orientation, history, solution,
selection, and dirty state unchanged. Move import likewise parses and validates
the complete sequence before applying it to a copy and committing the result.

## Atomic write policy

Explicit saves and autosaves use the same file service:

1. validate and serialize fully in memory;
2. create a unique temporary sibling in the destination directory;
3. write through a `FileStream` with exclusive sharing;
4. call `Flush(flushToDisk: true)` after the final byte;
5. if the destination exists, replace it while retaining a short-lived backup;
6. otherwise rename the temporary sibling to the destination on the same
   volume;
7. delete the backup only after the destination can be reopened and minimally
   validated;
8. remove only the temporary file created by this operation on failure.

The implementation must report when the filesystem cannot provide replacement
semantics; it must not delete the previous valid file first. A crash can leave
a recognizable temporary or backup file for recovery, never a knowingly
truncated destination.

## Validation diagnostics

Load APIs return a structured result containing severity, stable reason code,
safe message, and optional face/row/column or move-token position. Proposed
codes are:

| Code | Meaning |
| --- | --- |
| `fileNotFound` | Selected path no longer exists |
| `fileTooLarge` | Input exceeds the bounded format limit |
| `ioReadFailed` / `ioWriteFailed` | Safe I/O failure without path disclosure in telemetry |
| `invalidUtf8` | Text is not valid UTF-8 |
| `invalidJson` | JSON syntax is malformed |
| `formatMissing` / `formatMismatch` | Root format is absent or for another loader |
| `versionMissing` / `versionUnsupported` | Version cannot be interpreted safely |
| `sizeOutOfRange` | Cube size is outside supported bounds |
| `faceMissing` / `faceUnexpected` | Face set is not exactly U/R/F/D/L/B |
| `faceletCountMismatch` | A face is not exactly `N*N` entries |
| `unknownColor` / `colorCountMismatch` | Palette or total counts are invalid |
| `stateHashMismatch` | Supplied hash does not match canonical facelets |
| `statePhysicallyInvalid` | Later solvability validation rejects the state |
| `notationUnsupported` | Move convention is unknown |
| `moveTokenInvalid` / `moveOutOfRange` | Move syntax or layer is invalid |
| `historyUntrusted` | History cannot be proven to produce the state |
| `replaceFailed` | New file was prepared but destination replacement failed |

Messages shown in the UI may include the user-selected filename, but shared
logs and reports must not include unrelated absolute private paths.

## Implementation sequence

- Phase 04 adds append-only facelet access while preserving integer-cell ABI.
- Phases 05-06 make facelets and orientations rotate consistently.
- Phase 09 turns the state contract into JSON Schema and fixtures.
- Phase 10 implements transactional native serialization/deserialization.
- Phase 11 adds the managed atomic file service and WPF dialogs.
- Phases 12-14 add physical editing, physical validation, and recovery.

Until those phases land, the current text-box export remains explicitly
legacy/debug functionality and no arbitrary loaded state is claimed solvable.
