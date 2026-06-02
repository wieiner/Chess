# Chess3D AI Action Model

P3D adds a unified AI action descriptor for the five existing Chess3D RuleProfiles. It does not add a sixth mode.

## Action Kinds

- `Move`: normal piece move or capture.
- `LayerTurn`: Rubik ritual layer action when the profile enables it.
- `ReserveRestore`: reserve count restored to a legal home slot.
- `ProjectionCompositeMove`: Hodge primary move plus two mirror moves as one all-or-nothing action.

The model mirrors existing runtime actions. It does not create a new rules path.

## DTO Fields

`Chess3DAiActionDto` stores:

- action kind;
- side and macro-player;
- move from/to coordinates;
- reserve piece type and restore coordinates;
- layer axis/layer/quarter turn;
- Hodge primary side;
- score, flags, and result code.

Unused fields are set to neutral values such as `-1` or `0`.

## Source Of Truth

Candidate generation is based on the existing profile-aware legal-action diagnostic layer. That means Classic/Single candidates are king-safe, Rubik candidates include layer turns, Asgard candidates include reserve restore where legal, and Hodge candidates are projected composites.

## Mutation Contract

Building candidates and searching must not mutate board state, CoreCell stacks, fusion, reserve, action history, replay cursor, or state hash. Only `Chess3D_ApplyAiAction` and `Chess3D_MakeBestProfileAction` commit a selected action.
