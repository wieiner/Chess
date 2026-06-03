# Chess3D Search Hashing and Transposition Table

P3D.1 documents the transposition-table boundary but keeps TT implementation deferred.

A correct Chess3D search key must account for:

- ruleset id / profile;
- projected 512-cell board;
- current side and current macro-player;
- CoreCell stacks;
- reserve counts;
- game phase/outcome;
- enough anchor/fusion state to be recomputed consistently.

Action history and replay cursor should not affect the search key unless they affect legal state.

P3D.1 keeps copy-and-apply search without a TT because a partial key would risk incorrect reuse across stack/fusion/reserve/Hodge states. Future work can add a search-local bounded TT once state identity is formally hardened.

