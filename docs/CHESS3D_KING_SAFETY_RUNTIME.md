# Chess3D King Safety Runtime

P3A makes king safety runtime-backed for Classic Six-Side and Single-Side Training.

## Legal Filter

The engine still builds pseudo-legal 3D moves first. For king-safety profiles it then rejects moves that:

- capture a king directly;
- leave the moving side's king in check;
- move the king into an attacked cell.

If a side has no king on the board, the filter intentionally falls back to pseudo-legal behavior for compatibility with setup/debug tests and partial scenario construction.

## Check Truth

`Chess3D_IsSideInCheck` now finds the side king on the projected board and scans enemy attacks using the current movement geometry. This is runtime truth for Classic/Single-Side, not a draft text label.

## Outcome

For Classic/Single-Side when the current side has a king:

- in check and zero legal actions: `checkmate`;
- not in check and zero legal actions: `stalemate`;
- otherwise: active play.

The ABI outcome values are reused for compatibility; their public names now resolve to `checkmate` and `stalemate`.

## Non-Classic Isolation

Asgard, Rubik, and Hodge profiles keep their own outcome contracts. They may expose diagnostic summaries where safe, but Classic checkmate does not become their victory condition by accident.
