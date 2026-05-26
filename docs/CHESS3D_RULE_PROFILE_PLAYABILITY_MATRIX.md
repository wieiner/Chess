# Chess3D Rule Profile Playability Matrix

| Profile file | Ruleset id | Players | Setup | Moves | Captures | Turn model | Victory | Special actions | Engine implemented | UI exposed | Status |
|---|---|---:|---|---|---|---|---|---|---|---|---|
| `classic_six_side_3d_v0_1.json` | `classic-six-side-3d-8x8x8-v0.1` | 6 sides | six faces | classic 3D draft | classic remove | round robin | checkmate draft | none | movement/capture/action history/preview | selector, common panel, legal action list | playable-draft |
| `single_side_3d_v0_1.json` | `single-side-3d-8x8x8-v0.1` | 1 side | central 4x4 on Z0 | P2A movement core | classic remove | single-side loop | sandbox | none | movement/training preview | selector, common panel, legal action list | training |
| `asgard_convergence_3d_v0_1.json` | `asgard-convergence-3d-8x8x8-v0.1` | 6 sides | six faces plus core targets | classic 3D draft | knockback home/reserve outside core | round robin | centerAssembly anchors | core stacks, fusion, reserve restore | runtimePartial core/fusion/reserve/anchors | Asgard panel, legal action list, invalid reasons | experimental-playable |
| `rubik_convergence_3d_v0_1.json` | `rubik-convergence-3d-8x8x8-v0.1` | 6 sides | Asgard-style | classic 3D draft + layer action | knockback home/reserve outside core | round robin with layer turns | centerAssembly anchors | Rubik layer turns | projected board/core-stack rotation, fusion/anchor recompute | Rubik panel, action log, legal action list | experimental-playable |
| `hodge_projection_duel_3d_v0_1.json` | `hodge-projection-duel-3d-8x8x8-v0.1` | 2 macro players | six faces as two triads | projected composite moves | classic remove | macro alternating projection | sandbox/checkmate draft | Hodge projected move | all-or-nothing primary + mirrors | Hodge panel, preview, action log | experimental-playable |

Classic is not a forgotten fallback, and Asgard is not the default meaning of Chess3D. Rubik and Hodge are separate profile-gated modes.

## P2M Visual / Interaction Notes

- There are still exactly five real Chess3D RuleProfiles.
- The canonical OBJ/MTL model catalog is shared across profiles; it is not a new mode.
- Classic and Single-Side get the same improved readable materials and click diagnostics as experimental profiles.
- Asgard/Rubik/Hodge capabilities remain profile-gated through JSON and native runtime flags.
- Click-to-move now follows legal preview entries, so a highlighted target and the actual dispatched action use the same source/target/action-kind contract.
## P2N Reproducibility

All five real Chess3D profiles now have runnable headless playthrough JSON:

- Classic Six-Side
- Single-Side Training
- Asgard Convergence
- Rubik Convergence
- Hodge Projection Duel

These files are scenarios, not additional RuleProfiles. The real Chess3D RuleProfile count remains exactly five.
