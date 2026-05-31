# Chess3D Rubik Visual Language

Rubik layer visuals are enabled only for `rubik_convergence_3d_v0_1.json`.

- Axis/layer/quarter controls show the intended turn before execution.
- The selected layer gets a translucent wash before the engine applies the turn.
- Input is briefly locked during the visual pre-highlight.
- The engine applies `RotateLayer` exactly once per quarter turn.
- After the turn, board, CoreCell stacks, fusion, anchors, action log, and state hash are refreshed from engine state.

Non-Rubik profiles should show disabled/collapsed layer controls or clean-fail without mutation.
