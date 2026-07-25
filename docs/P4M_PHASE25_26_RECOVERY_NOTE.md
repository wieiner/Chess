# P4M Phase 25/26 Recovery Note

## Recovered topology

- Local branch: `main`.
- Recovered local Phase 25 commit: `c98e8aa6df49e7ab285b87d8e36d5eba0843770e`.
- Origin before recovery: `2e77e48fedd97b98daeebfcdc3353e67d38d6883`.
- Phase 25 was the local `HEAD`, was inspected, and was pushed normally. No
  cherry-pick, force push, reset, clean, checkout, restore, or stash was used.

## Preserved Phase 26 work

The dirty tree contained only intended text assets:

- tracked source/runtime namespace README files;
- `docs/P4M_MODEL_ASSET_LAYOUT.md`;
- the Phase 26 research-log entry.

No binary, archive, generated output, runtime log, absolute workstation path,
license claim, or temporary conversion result was present. The recovered files
remain suitable for tracking.

Phase 26 additionally adds `scripts/assets/Initialize-ModelInbox.ps1` and the
approved-source root policy. The script creates raw drop directories only
below ignored `rude-resource/model-inbox`.

## Runtime artifacts

No runtime artifact needed removal. Existing `assets/models/chess/pieces/default`
content was not moved or renamed.

## Closure plan

1. Prove the inbox is ignored and can be initialized idempotently.
2. Confirm no raw archive or Blender cache is tracked.
3. Confirm existing OBJ/MTL runtime paths remain unchanged.
4. Confirm the five Chess3D profile JSON files remain unchanged.
5. Commit the complete Phase 26 scope explicitly.
