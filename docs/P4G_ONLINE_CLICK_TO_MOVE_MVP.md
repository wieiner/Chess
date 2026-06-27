# P4G Online Click-To-Move MVP

Date: 2026-06-27

`ChessOnlineApp` now has a minimal board-driven move submit path for online snapshots.

## User Flow

1. Create/start an online test match.
2. Request or receive a server snapshot.
3. Select a board cell in the P4G snapshot grid.
4. Click **Use Selected as From**.
5. Select a target cell.
6. Click **Use Selected as To**.
7. Click **Submit Normal Move**.

The UI builds an `OnlineActionCommand`:

- `ActionKind = NormalMove`;
- `ActorSide` from the selected source piece side;
- source and target coordinates from the clicked cells;
- `ExpectedStateHashBefore` from the latest authoritative snapshot.

## Server Authority

The client does not locally apply the move. The command is sent to the server and can be:

- accepted: action counter/log update, then a fresh authoritative snapshot is requested and rendered;
- rejected: rejection reason is shown and the local board remains unchanged.

This keeps the board consistent with the Linux-native authority and the server state hash.

## Visual Feedback

- selected cell: blue;
- move source: green;
- move target: amber;
- occupied cells show compact labels such as `S1P`.

## Limitations

- This is not yet full legal target preview.
- It submits only `NormalMove`; Rubik layer turns, Hodge projected moves, and reserve restore stay in their dedicated controls for now.
- Wrong-side or illegal moves are detected by the server and displayed as rejection text.
- Public HTTP 80 remains diagnostic/dev only.
