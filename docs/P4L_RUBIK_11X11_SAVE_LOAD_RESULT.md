# Rubik 11x11 Save/Load Result

## Automated scenarios

`RubikStateContractTests` runs five independent 11x11 physical-state file
roundtrips: solved, deterministic 12-move scramble, inner slice, two-layer wide
move, and a whole-cube rotation represented by all eleven Y layers.

Each scenario performs native mutation, canonical document creation, atomic
save, bounded read, hash verification, apply to a fresh native handle, and
native facelet export. The source and loaded arrays and hashes match exactly.

The visual comparison uses the same `RubikVisualDescriptorBuilder` fallback as
RubikApp and compares every `(coordinate, world face, color)` sticker. Every
loaded scenario renders 726 stickers, zero invalid descriptors, and the same
shell signature.

## Honest import boundary

A `.rubik.json` document stores facelets, not trusted move history or a proved
cubie decomposition. After load the native state therefore reports manual
state, zero history, and unavailable orientation. RubikApp displays the
facelet-shell fallback rather than inventing cubie IDs. Phase 18 owns future
decomposition; until then reverse-history solving remains unavailable for
imported physical files.

Invalid document and injected file failures retain the prior state/file as
covered by the Phase 12/13 contract matrix. The legacy integer text path stays
separate and cannot be promoted to a portable physical state.
