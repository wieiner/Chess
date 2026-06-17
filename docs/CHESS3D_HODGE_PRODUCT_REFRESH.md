# Chess3D Hodge Product Refresh

Phase: P4C phase 09.

## Identity

Hodge Projection Duel is one of the five real Chess3D RuleProfiles:

- file: `assets/rules/profiles/hodge_projection_duel_3d_v0_1.json`
- rulesetId: `hodge-projection-duel-3d-8x8x8-v0.1`
- product role: two macro-player projected-composite duel

Hodge is not Asgard. It defaults to exclusive occupancy, classic capture, no CoreCell stacks, no fusion, no reserve, and no Rubik layer turns.

## Runtime Capabilities

- Two macro-players each own three projection sides.
- A legal action is an all-or-nothing projected composite move.
- The primary move and two mirror moves succeed or roll back together.
- Action notation includes HPD/projection information.
- Legal preview, state hash, save/load, and replay use the same action-history foundation as other profiles.

## Online / Matchmaking

Online authority accepts `HodgeProjectedMove` only under the Hodge profile. Classic, Single-Side, Asgard, and Rubik must reject it.

P4C phase 09 adds explicit SignalR matchmaking smoke for the Hodge ruleset so exact-profile queues cover macro-player modes too.

## AI / Replay / Diagnostics

- AI/search can produce projected composite candidates.
- Blocked-mirror rollback is covered by regression fixtures.
- Replay records one composite action rather than pretending three independent players moved.

## Known Gaps

- Hodge remains experimental-playable.
- No deep Hodge-specific AI.
- No public ranked matchmaking.
- Online serialization is protocol-stable for current actions but not a public anti-cheat system.
