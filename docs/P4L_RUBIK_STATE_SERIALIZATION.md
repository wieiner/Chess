# Rubik State Serialization

## Boundary

`src/RubikState` is a WPF-independent `net8.0` library. It owns the portable
v1 document model, strict parser, validator, canonical SHA-256 calculation,
and immutable native load plan. It does not own a live native cube or a file
path.

## Read pipeline

1. Reject input above the configured byte limit.
2. Parse UTF-8 JSON with comments/trailing commas disabled and bounded depth.
3. Reject duplicate and unknown properties.
4. Require the exact v1 root, face order, color scheme, and six faces.
5. Normalize canonical color names or IDs to integer IDs `1..6`.
6. Validate `N*N` face lengths and `N*N` occurrences of every color.
7. Calculate the canonical hash and compare a supplied non-empty hash.
8. Return `RubikStateLoadPlan` containing copied normalized facelets.

No native callback is reachable before step 8. The UI file flow creates and
populates a temporary native cube from this plan; the current cube is replaced
only after native acceptance. Invalid input therefore cannot partially mutate
the current cube.

## Write pipeline

Serialization validates the document, recalculates its hash, writes root and
face properties in canonical order, and sorts metadata object properties.
Timestamps, source labels, metadata, whitespace, and JSON property order do not
affect the hash.

The parser rejects absolute paths and executable metadata. It performs no
polymorphic loading, external reference resolution, script execution, or file
access.

## Contract coverage

`RubikStateContractTests` covers roundtrips through native facelets for sizes
2, 3, 4, 8, 11, and 32. Negative cases include truncated/duplicate/oversized
JSON, unsupported version/size, missing/extra/short faces, invalid color/count,
hash mismatch, unknown root members, absolute paths, and executable metadata.
