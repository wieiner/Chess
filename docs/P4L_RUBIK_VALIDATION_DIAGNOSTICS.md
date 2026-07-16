# Rubik Validation Diagnostics

Physical input validation now returns `RubikValidationIssue` values with
severity, stable code, optional face/row/column and cubie class, explanation,
and suggested action. Basic reason codes cover face shape, unknown/missing
colors, per-color underflow/overflow, duplicate inventory (reserved for cubie
decomposition), center scheme, hash, and version boundaries.

The face editor lists issues. Selecting an addressed issue opens its face tab,
scrolls to the cell, and focuses it. Cell-level missing-sticker detail is
bounded to keep an empty 32x32 draft responsive; complete aggregate count
issues are always retained.

`Export validation report` writes a sanitized JSON report containing size,
summary counts, and structured issues. It excludes facelet payloads, source
paths, clipboard text, and file contents. These diagnostics prove basic input
validity only; cubie inventory, orientation, permutation, and parity are owned
by Phases 18 and 19.
