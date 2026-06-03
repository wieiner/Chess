# Chess3D Iterative Deepening

P3D.1 wraps profile-aware search in iterative deepening.

The engine searches depth `1`, then `2`, and so on up to the clamped requested depth. After every completed depth, the best action and score are stored as the last completed result. If a node or time limit interrupts a deeper search, the engine returns the previous completed result. If no depth completed, the search fails cleanly without mutating game state.

The summary JSON reports:

- requested depth;
- effective depth;
- completed depth;
- stopped reason;
- elapsed time;
- nodes and qnodes;
- candidate count and ordered candidate count;
- best action compact text.

Depths are intentionally shallow. P3D.1 is a correctness and diagnostics gate, not a tournament engine.

