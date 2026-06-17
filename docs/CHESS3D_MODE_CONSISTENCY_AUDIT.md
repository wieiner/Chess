# Chess3D Mode Consistency Audit

Status: P4C Phase 07.

## Real Chess3D RuleProfiles

There are exactly five real Chess3D RuleProfiles:

1. `classic_six_side_3d_v0_1.json`
2. `single_side_3d_v0_1.json`
3. `asgard_convergence_3d_v0_1.json`
4. `rubik_convergence_3d_v0_1.json`
5. `hodge_projection_duel_3d_v0_1.json`

`chess3d_rule_profile.schema.json` is a schema file, not a mode.

Scenario, playthrough, regression, online, SignalR, identity, persistence, matchmaking, Asgard-online, and deployment JSON files are fixtures or descriptors. They are not RuleProfiles.

## Product Surfaces

| Surface | Mode/product role | RuleProfile? | Notes |
| --- | --- | --- | --- |
| Chess2D | Orthodox 2D chess/advisor | no | Separate product surface using the 2D engine. |
| Chess3D Classic Six-Side | Base six-side 8x8x8 game | yes | Classic, not Asgard. |
| Chess3D Single-Side | Training and movement sandbox | yes | Useful for setup, UI, and movement validation. |
| Asgard / Meru Convergence | Core stacks/fusion/reserve/anchors | yes | Center/core mode, not the default meaning of Chess3D. |
| Rubik Convergence | Asgard-like mode plus layer turns | yes | Layer turns are profile-gated. |
| Hodge Projection Duel | Two macro-player projection duel | yes | Projection mode, not Asgard. |
| RubikApp | Standalone Rubik app | no | Separate executable/product. |
| ChessOnlineServer | Hosted authority transport | no | Server surface for online play; it hosts existing profiles. |
| ChessOnlineApp | Desktop online/operator client | no | UI/control surface for online integration. |

## Consistency Findings

- Classic remains first-class and should not be described as a fallback.
- Single-Side remains a training profile, not a competitive full match profile.
- Asgard is a profile with core mechanics; it must not absorb all Chess3D language.
- Rubik is a separate profile because layer turns are not legal in Asgard by default.
- Hodge is a separate profile because projected composite moves are not legal in Classic/Asgard/Rubik.
- Online matchmaking is exact-profile; it does not create new modes.
- AI/search is profile-aware; it does not change profile rules.
- Save/load/replay/action history are infrastructure layers shared by profiles.

## Mode-Specific Gaps

| Mode | Known gap |
| --- | --- |
| Classic Six-Side | Product balancing and deeper full-game QA remain ongoing. |
| Single-Side | It is intentionally a training/sandbox surface. |
| Asgard | Final destructive fusion/implosion rules remain future work. |
| Rubik | Rich player UX for layer-turn planning remains future work. |
| Hodge | Projection UX, AI depth, and online polish remain future work. |

## Online / Matchmaking Status

- Classic: matchmaking smoke exists.
- Asgard: matchmaking and legal action smoke exists.
- Rubik: online layer-turn action is supported by the authority path.
- Hodge: online projected composite action is supported by the authority path.
- Single-Side: profile is accepted by catalog and uses one-player matching policy.

Queued matchmaking tickets are in-memory P4C state. Match-found room/table/seat assignments are persisted after Phase 06.

## RuleProfile Counting Rule

When counting modes, use this rule:

- Count JSON files that are RuleProfile instances with a `rulesetId` for gameplay.
- Do not count schemas.
- Do not count scenario/playthrough/regression/online/deployment fixtures.
- Do not count executables as RuleProfiles.

This keeps the Chess3D RuleProfile count at exactly five.
