# Chess3D Hit-Test And Selection Hardening

P3B keeps click-to-move tied to legal preview.

## Policy

- Clicking a piece or tile selects the logical cell.
- Clicking a highlighted target dispatches the exact preview action.
- Overlay models are not registered as logical hit targets, so hit testing continues through them.
- Failed clicks show a reason and do not mutate board, action history, replay cursor, reserve, stacks, or state hash.

## Lifecycle

Profile load, reset, clear, save/load import, replay import, and manual clear-selection remove stale selection and preview overlays. During animations, input is locked and the status text explains why a click is ignored.
