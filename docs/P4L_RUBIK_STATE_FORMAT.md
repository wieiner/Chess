# Portable Rubik State Format

## Identity

- Extension: `.rubik.json`
- `format`: `rubik.state`
- major `version`: `1`
- encoding: UTF-8 JSON
- supported size: 2 through 32
- canonical face order: `U`, `R`, `F`, `D`, `L`, `B`

The document represents physical facelets. It is independent of native cubie
IDs, renderer objects, move history, UI state, and local paths.

## Required data

`faces` contains six row-major arrays. Every face must contain exactly `N*N`
values. A value may be a compact color ID or canonical name:

| ID | Name | Solved face |
| ---: | --- | --- |
| 1 | white | U |
| 2 | red | R |
| 3 | green | F |
| 4 | yellow | D |
| 5 | orange | L |
| 6 | blue | B |

After normalization, each color must occur exactly `N*N` times across the
document. JSON Schema provides structural/bounded validation; runtime
validation enforces size-dependent lengths, multiplicity, duplicate-property
rejection, and hash equality.

Unknown root members are rejected. Optional forward-compatible data belongs
inside `metadata`; it is never executable and never affects physical state.

## Canonical hash v1

The lowercase SHA-256 fingerprint is calculated over UTF-8 bytes of this ASCII
material with no trailing newline:

```text
rubik.state|1|N|U=white,R=red,F=green,D=yellow,L=orange,B=blue|<comma-separated numeric U/R/F/D/L/B facelets>
```

Whitespace, property order, `createdUtc`, `source`, `metadata`, UI state, local
path, and move history are excluded. The hash is a deterministic corruption
and reproduction fingerprint, not a security signature.

## Compatibility and trust

- Unsupported major versions are rejected transactionally.
- A missing/empty hash is allowed for authoring and is populated on save.
- A supplied non-empty hash must match normalized state.
- History is intentionally absent; `.rubikmoves` remains a separate artifact.
- Successful count validation does not prove physical solvability. Cubie/parity
  proof is a later validation level.

Schema: `assets/rules/rubik/rubik-state-v1.schema.json`.
