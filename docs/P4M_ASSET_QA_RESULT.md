# P4M Enabled Asset-Set QA Result

Status: **BLOCKED - license and provenance decision required**

Date: 2026-07-25

## Scope

The runtime catalog currently exposes one physical model set:

| Set | Format | Apps | Runtime status |
| --- | --- | --- | --- |
| `default-obj` | OBJ/MTL, v1 adapter | Chess2D, Chess3D | Legacy compatibility set, selected by default when present |

No approved v2 GLB set or Rubik override is present. Chess2D, Chess3D, and
Rubik retain procedural fallback models.

## Technical checks

| Check | Result | Evidence |
| --- | --- | --- |
| Catalog/schema parse | PASS | v1 adapter produces a strict in-memory v2 view |
| Required Chess2D roles | PASS | 12 pieces plus two board tiles |
| Paths and files | PASS | all 14 referenced OBJ files exist under the package root |
| SHA-256 | PASS | adapter computes hashes from current tracked files |
| Geometry validation | PASS | bounded OBJ contracts and existing application loaders |
| Size policy | PASS | largest OBJ is below the Phase 28 review threshold |
| GLB path | NOT APPLICABLE | no enabled GLB package |
| OBJ fallback | PASS | ChessApp and Chess3DApp targeted builds |
| Procedural fallback | PASS | no set, missing role, corrupt GLB, and Rubik fallback contracts |
| App compatibility | PASS (technical) | Chess2D/Chess3D catalog integration and Rubik optional boundary |
| Source/runtime separation | PASS | no raw FBX, Blend, ZIP, inbox, or `.tmp` package input |
| License | **BLOCKED** | adapter reports `NOASSERTION / pending-review` |
| Author/source provenance | **BLOCKED** | no author, source URL, source hash, or acquisition record |
| Private paths | **FAIL** | six white MTL files contain an absolute `E:/...` texture path; six black MTL files contain `map_Kd .` |

## Why this is a blocking result

SPDX defines `NOASSERTION` as a known absence of a defensible license
determination; no permission should be inferred from it. A public repository
also does not itself grant redistribution rights to unlicensed files. Therefore
the current legacy models cannot be declared approved or satisfy the Phase 42
`no unlicensed asset` gate.

The models were migrated from an earlier local application directory. That is
useful repository history, but it does not establish authorship or the right to
redistribute the meshes and derived materials.

## Required owner evidence

To unblock the set, provide one of:

1. confirmation that the repository owner authored every mesh/material, plus
   the chosen license and an author/provenance statement;
2. the original source URL/package and its license/receipt terms showing that
   redistribution of these runtime derivatives is permitted;
3. a replacement set with clear author, source, SPDX-compatible license, and
   role mapping.

After that decision, the next safe change is to add a license/notice file,
record source metadata, sanitize the twelve invalid `map_Kd` entries, regenerate
hashes/manifest evidence, validate, preview, and rerun packaging.

## Gate decision

Phase 41 is not green. Phases 42 and 43 are intentionally not claimed complete.
No license was invented, no model was deleted, and no existing runtime behavior
was changed as part of this QA decision.
