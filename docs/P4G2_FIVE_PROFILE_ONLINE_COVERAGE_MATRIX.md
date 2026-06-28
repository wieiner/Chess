# P4G2 Phase 21 - Five-Profile Online Coverage Matrix

Date: 2026-06-28

## Scope

This matrix records current online coverage for the exactly five real Chess3D RuleProfiles. Scenario, playthrough, and regression JSON files are not game modes.

## Remote Coverage Summary

| Profile | Match starts | Snapshot render/data | Legal preview | Normal move accepted | Special action status | Two-window tested | Limitation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `classic-six-side-3d-8x8x8-v0.1` | PASS | PASS | PASS | PASS via `server-preview` | No special action | PASS, Classic default | Full 3D visual polish remains future work. |
| `single-side-3d-8x8x8-v0.1` | PASS | PASS | Not submitted in remote matrix smoke | Not submitted in matrix smoke | Training one-player profile | Not separately two-window tested | Smoke tool now handles one-player immediate `MatchFound`; action submit intentionally skipped in matrix pass. |
| `asgard-convergence-3d-8x8x8-v0.1` | PASS | PASS | PASS | PASS via `server-preview` | Core/fusion/reserve UX remains profile-specific | One-app backend smoke via server preview; UI tested on Classic click path | Special reserve/fusion UI is not treated as normal move. |
| `rubik-convergence-3d-8x8x8-v0.1` | PASS | PASS | Startup/snapshot only in matrix smoke | Skipped | Layer turn remains special action | Not separately two-window tested | Layer-turn UI/action boundary remains future P4G/P5 follow-up. |
| `hodge-projection-duel-3d-8x8x8-v0.1` | PASS | PASS | Startup/snapshot only in matrix smoke | Skipped | Projection composite remains special action | Not separately two-window tested | Hodge projected action UI is not mapped to normal move. |

## Fresh Remote Smoke Evidence

Classic full action:

```text
STEP PASS matchmaking room=match-12-classic table=table-12
action-source=server-preview
notation=#1 S1 MOVE K (4,4,0)->(3,5,1)
SMOKE PASS
```

Asgard full action:

```text
STEP PASS matchmaking room=match-11-asgard table=table-11
action-source=server-preview
notation=#1 S1 MOVE R (2,2,0)->(1,2,0)
SMOKE PASS
```

Single-side startup/snapshot:

```text
STEP PASS matchmaking room=match-13-single table=table-13
STEP PASS game start hash=e3a0ccbe33ae47df
STEP SKIP action submit skipped by --skip-action-submit
STEP PASS snapshot/actionlog finalHash=e3a0ccbe33ae47df
SMOKE PASS
```

Rubik startup/snapshot:

```text
STEP PASS matchmaking room=match-14-rubik table=table-14
STEP PASS game start hash=df5cecc8f5e0c331
STEP SKIP action submit skipped by --skip-action-submit
STEP PASS snapshot/actionlog finalHash=df5cecc8f5e0c331
SMOKE PASS
```

Hodge startup/snapshot:

```text
STEP PASS matchmaking room=match-15-hodge table=table-15
STEP PASS game start hash=668481062bf778bd
STEP SKIP action submit skipped by --skip-action-submit
STEP PASS snapshot/actionlog finalHash=668481062bf778bd
SMOKE PASS
```

## Tooling Note

The smoke tool now treats `single-side-3d-8x8x8-v0.1` as a one-player matchmaking profile. Its first `JoinMatchmaking` can return `MatchFound` immediately instead of `MatchmakingJoined`, which is correct for this profile.

## Boundary

No sixth profile was added. Rubik layer turns, Hodge projection composites, and Asgard reserve/fusion mechanics remain explicit special actions and are not silently submitted as normal moves in the coverage matrix.
