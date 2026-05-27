# Chess3D P3A Divide Fixtures

P3A does not introduce large published perft tables. It adds small deterministic fixtures that prove the legal filter is in the divide/perft path.

Covered fixture ideas:

- self-check is absent from legal roots;
- king-into-check is absent from legal roots;
- capture-checker and block-check positions keep legal resolving moves;
- checkmate and stalemate positions expose zero legal actions.

The exact node counts are intentionally small and contract-test oriented so CI stays fast.
