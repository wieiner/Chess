# Chess3D AI UI

P3D adds a compact AI / Search panel to the Chess3D control center. P3D.1 keeps the same panel and makes bounded search/apply operations asynchronous so the WPF UI stays responsive during deeper searches.

## Controls

- `Depth`: shallow search depth.
- `Nodes`: node limit.
- `ms`: soft time limit.
- `Candidates`: build and display candidate summary.
- `Search Best`: search without mutating the game.
- `Make AI Move`: search and apply one best profile-aware action.
- `Copy Summary`: copy the latest AI summary JSON.

## Display

The panel shows the best action in readable form and the native summary JSON. P3D.1 summary JSON includes completed depth, nodes, qnodes, cutoffs, elapsed time, stopped reason, and compact best-action text. Existing action log, state hash, visual refresh, and invalid reason panels remain the source of truth for committed state.

## Scope

The UI does not add a new mode, search timeline, or AI-vs-AI scheduler. It exposes the new native search ABI for manual play and regression reproduction.

## P3E Online Boundary

P3E can build an AI candidate command in the local authority harness for smoke tests, but remote/server AI play is not enabled as a product feature. Production multiplayer AI scheduling, AI seats, and hosted search policies remain future work.
