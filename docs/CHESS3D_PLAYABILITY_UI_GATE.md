# Chess3D Playability UI Gate

P2O strengthens the Chess3D control center by making rule state visible, not by adding new visual modes.

## Always Visible

The status/control panels surface:

- active RuleProfile;
- current side and macro-player;
- game phase and outcome;
- allowed action mask;
- selected cell and selected piece;
- legal action count;
- last invalid action reason;
- state hash;
- mode rule summary;
- check status summary when applicable.

## Action Dispatch

The board UI continues to use legal preview as the source of truth. A target click must match an exact preview entry before dispatch. Failed actions keep board/action history unchanged and show a reason.

## Game Over

P2O exposes `GamePhase` and `GameOutcome` through ABI so the UI can show completion status. CenterAssembly completion is runtime for Asgard/Rubik, and P3A makes Classic/Single-Side checkmate and stalemate runtime-backed.

## Deferred UI

Animated Rubik turns, stack/fusion cinematic visualization, replay timeline controls, and AI/search panels remain later work.

## P3C Visual RC Gate

P3C adds a release-candidate visual control surface:

- explicit visual state snapshot;
- camera presets and readability toggles;
- profile-gated overlay toggles;
- visual diagnostics copy/refresh;
- short animation lock contract for action flashes, replay step, Rubik layer highlight, and Hodge mirror paths.

The gate remains honest: manual visual QA is required, and the engine is still the only authority for legal actions and outcomes.
