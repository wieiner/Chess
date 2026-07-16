# P4L Rubik Final Report

Date: 2026-07-16

## Result

P4L-R closes the Rubik visual/physical-state foundation and ships a capability-aware solver workflow without claiming unavailable arbitrary NxN solving.

1. The one-color cubie renderer is fixed: body and physical stickers are separate visual models.
2. Corner descriptors/models expose three distinct sticker colors.
3. Edge/wing descriptors/models expose two distinct sticker colors.
4. Native cubie orientation is an exact integer signed-axis basis stored per current position and rotated with the cubie. Facelet-only imports render through an explicit world-face shell fallback and do not fabricate orientation.
5. Engine/state workflows are covered across N=2,3,4,5,7,8,11 as appropriate; the portable format/UI bounds are N=2..32.
6. N=11 rendering works and is covered by visual evidence, performance checks, descriptor tests, turns, and state roundtrips.
7. `.rubik.json` is saved to the path selected in `Save State`/`Save As`; replacement can retain a sibling `.bak`.
8. `Load State` performs bounded strict parsing and transactional candidate-handle application.
9. `Physical Editor` provides six labelled face grids, draft-only editing, undo/redo, orientation guidance, structured validation, and explicit apply.
10. Validation detects structural/count/hash errors, piece inventory errors, 2x2/3x3 corner twist, 3x3 edge flip, and 3x3 permutation parity. Full N>3 parity proof remains unimplemented and is reported as such.
11. Solver implementations are trusted-history reverse, owned bounded arbitrary 2x2 IDDFS, and an owned NxN reduction framework.
12. Arbitrary imported 2x2 solving works within explicit bounds and is independently native replay-verified.
13. Arbitrary 3x3 solving is not implemented; only full small-cube validation and move simulation are available.
14. N=11 reached Level A only: import/load, validation, decomposition, reduction guidance, checkpoint, and an explicitly incomplete/unverified move artifact. Centers/wings are not solved.
15. Every candidate called a complete solution by the new workflow is independently replayed on a fresh native engine, checked solved, and assigned a final hash. Partial NxN artifacts are never marked verified.
16. Verified solutions can be saved/loaded as atomic versioned `.rubikmoves` documents and played/stepped in the UI.
17. NxN checkpoints can be atomically saved/loaded and validated by API; the compact UI currently displays checkpoint status but has no dedicated checkpoint file buttons.
18. Old integer-cell ABI, facelet ABI, notation, scramble, layer/wide/whole-cube turns, trusted reverse-history solve, and animated playback are preserved.
19. Phase 28 Rubik gate passed 170 native, 68 visual, and 335 state assertions. Phase 29 full runner and `scripts/verify.ps1` passed, including quick benchmark and packaging.
20. The final commit is `P4L phase 30: finalize physical Rubik workflow` (hash recorded by Git after this report is committed).
21. Final GitHub Actions run is recorded in the user-facing completion report after push.
22. The working tree is required to be clean after the final push.
23. No ChessOnlineServer, Hetzner deployment, nginx, systemd, firewall, TLS, runtime store, or server secret was touched.

## Evidence

- `P4L_RUBIK_FULL_VERIFY_RESULT.md`
- `P4L_RUBIK_REPOSITORY_REGRESSION.md`
- `P4L_RUBIK_VISUAL_EVIDENCE_RESULT.md`
- `P4L_RUBIK_11X11_SAVE_LOAD_RESULT.md`
- `P4L_RUBIK_11X11_SOLVER_RESULT.md`
- `P4L_RUBIK_SOLVER_WORKFLOW.md`

## Remaining work

- approve/implement an arbitrary 3x3 backend with clear licensing and bounded resources;
- implement NxN center solving, wing pairing, reduced-3x3 solving, and parity corrections before claiming arbitrary N=11 solve;
- add dedicated checkpoint file controls and broader manual accessibility/visual QA;
- keep server/online work in its own phases.
