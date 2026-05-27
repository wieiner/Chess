# Chess3D Rubik Layer Turn Animation

P3B adds a lightweight WPF animation guard around Rubik layer turns.

## Flow

1. UI calls `CanRotateLayer`.
2. If the profile does not allow layer turns, the action fails without mutation and shows a reason.
3. If allowed, the selected layer is highlighted for a short interval.
4. The engine `RotateLayer` ABI is called exactly once after the visual pre-highlight.
5. The board is rebuilt from engine state and fusion/anchor/status panels refresh.

The legacy 180-degree UI button is treated as two quarter turns, producing two normal layer-turn actions instead of inventing a new engine action.

## Safety

Input is locked during the short animation window. Reserve counts are not moved. Asgard, Classic, Single-Side, and Hodge profiles remain disabled for layer turns unless their profile says otherwise.
