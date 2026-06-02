# Chess3D AI UI

P3D adds a compact AI / Search panel to the Chess3D control center.

## Controls

- `Depth`: shallow search depth.
- `Nodes`: node limit.
- `ms`: soft time limit.
- `Candidates`: build and display candidate summary.
- `Search Best`: search without mutating the game.
- `Make AI Move`: search and apply one best profile-aware action.
- `Copy Summary`: copy the latest AI summary JSON.

## Display

The panel shows the best action in readable form and the native summary JSON. Existing action log, state hash, visual refresh, and invalid reason panels remain the source of truth for committed state.

## Scope

The UI does not add a new mode, search timeline, or AI-vs-AI scheduler. It exposes the new native search ABI for manual play and regression reproduction.
