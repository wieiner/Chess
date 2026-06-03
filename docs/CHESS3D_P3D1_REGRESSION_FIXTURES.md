# Chess3D P3D.1 Regression Fixtures

P3D.1 adds regression fixtures under `assets/rules/scenarios/chess3d/regression`.

The fixtures cover:

- iterative depth-2 smoke;
- tiny node limit no-mutation;
- quiescence-lite capture/recapture smoke;
- deterministic candidate ordering;
- Asgard anchor/fusion/reserve scoring smoke;
- Rubik layer-turn ordering and four-turn safety;
- Hodge macro-player composite search and timeout/no-partial-apply behavior;
- parseable summary JSON v2 across all five profiles;
- repeated search with no action-history growth.

They are runnable by the existing headless playthrough/regression runner and do not represent new game modes.

