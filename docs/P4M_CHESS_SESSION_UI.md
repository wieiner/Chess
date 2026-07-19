# Chess2D Session UI

The **Партия** tab keeps PGN and application sessions as separate workflows.

- **Save Session** updates the current `*.chesssession.json` file.
- **Save Session As** selects a new file.
- **Load Session** validates a candidate before replacing the live board/history.
- **Recent Sessions** lists up to eight files opened during the current application run.

The green session status line shows the filename, `*` dirty marker, the first 12 characters of the deterministic session hash, save/load status, and recovery status. PGN remains the interchange format; session files additionally restore orientation, theme/model-set selection, 2D/3D mode, and basic search limits.

A load first validates JSON, version, bounds, FEN chain, and semantic identifiers. It then creates candidate native/history state. The current window is changed only after candidate validation, with a rollback snapshot retained for the final apply boundary.
