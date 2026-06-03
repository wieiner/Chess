# Chess3D Move Ordering

P3D.1 orders AI candidates deterministically before search. Ordering improves alpha-beta behavior but does not remove legal candidates.

The ordering score considers:

- resulting static evaluation from the root actor perspective;
- captures and rough material swing;
- reserve restore actions;
- Asgard anchor/fusion/implosion progress;
- Rubik layer actions as legal profile actions;
- Hodge projected composite actions as one action;
- stable tie-breaks by action kind, side, coordinates, axis/layer, and quarter turn.

No randomness is used. Repeated candidate generation on the same state produces the same ordered action list.

