# P4L Phase 07 Recovery Note

## Recovered baseline

- Original `HEAD` and `origin/main`: `0b277b06fc6b4396d758ef6f0aeb735174b37c1b`.
- Branch: `main`.
- The working tree contained only the intended unfinished Phase 07 work.
- No reset, clean, restore, or stash operation was used.

## Recovered files

- `src/RubikApp/MainWindow.xaml.cs`: dark plastic bodies, separate sticker quads, grouped animation, child-model hit testing, and render diagnostics.
- `docs/P4L_RUBIK_STICKER_RENDERING.md`: renderer design and behavior.
- `docs/NEXT_ERA_MICRO_RESEARCH_LOG.md`: Phase 07 primary-source notes.

The untracked screenshots were confined to ignored `.tmp/visual-smoke`. They are
diagnostic artifacts and are not part of the commit.

## Completion work

The recovered renderer initially derived sticker count and direction only from
the cubie's current boundary coordinate. Phase 07 completion now uses the
Phase 06 physical sticker mask for sticker identity and the exact integer
orientation basis for world normals. Current facelets remain the color
authority. A facelet-only imported state uses an explicit shell fallback and
reports that cubie orientation is unavailable instead of inventing it.

## Repeated verification

Phase 07 repeats sequential native/app builds and the dedicated Rubik contract
suite. Visual proof is moved to the deterministic descriptor and in-process
capture phases so desktop foreground automation is not treated as evidence.
