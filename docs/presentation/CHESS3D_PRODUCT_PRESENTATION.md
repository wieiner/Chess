# Chess3D Product Presentation

Status: P4C Phase 11 product deck.

This is a presentation-ready outline for the current product surface. It is deliberately honest: Chess3D has five real RuleProfiles, several strong runtime systems, and a Windows-hosted online prototype. It does not claim a sixth mode, public ranked service, Linux-native authority, Redis/Azure SignalR scale-out, or final Asgard/Rubik/Hodge balance.

## Slide 1 - Chess3D Platform

Chess3D is a multi-profile 8x8x8 chess platform with playable rules, replayable actions, visual assets, AI/search smoke, and a hosted SignalR authority prototype.

Core message:
- one engine family;
- five distinct Chess3D profiles;
- shared action, replay, visual, AI, and online layers;
- profile-gated behavior instead of one mode pretending to be all modes.

## Slide 2 - Five Real Profiles

| Profile | Product role |
| --- | --- |
| Classic Six-Side | normal king-safe 8x8x8 chess profile |
| Single-Side Training | one-side training and movement sandbox |
| Asgard / Meru Convergence | Forbidden Core, stacks, fusion descriptors, reserve, anchors |
| Rubik Convergence | Asgard-like profile plus legal layer turns |
| Hodge Projection Duel | two macro-players with projected composite moves |

Scenario, regression, identity, deployment, online, and generated-asset JSON files are support data. They are not game modes.

## Slide 3 - What Makes It Playable

- Profile selector and capability summaries.
- Legal action preview and invalid-action reasons.
- Click-to-move dispatch through exact preview entries.
- Action history and deterministic notation.
- Save/load/replay and state-hash diagnostics.
- Headless playthrough and regression fixtures.

## Slide 4 - Classic / Single-Side

Classic Six-Side is the baseline playable chess profile.

Implemented:
- 8x8x8 board;
- king safety;
- check/checkmate/stalemate;
- legal action preview filtered by king safety;
- action perft/divide diagnostics;
- online authority smoke.

Single-Side is a training profile. It uses the shared movement and king-safety kernel where applicable, but it is not presented as a public competitive mode.

## Slide 5 - Asgard / Meru

Asgard is a separate convergence profile. It is not the default identity of Chess3D.

Implemented:
- Forbidden Core at x/y/z 2..5;
- CoreCell stacks;
- non-destructive fusion descriptors;
- contested state;
- implosion progress descriptor;
- knockback/reserve capture;
- reserve restore to valid home slots;
- centerAssembly anchors and victory path.

Deferred:
- destructive implosion;
- final Volume-Surface 216 rule;
- dislodge/knockback from core;
- final balance and UI drama.

## Slide 6 - Rubik Convergence

Rubik Convergence adds legal layer turns to the Asgard-like stack/fusion state.

Implemented:
- X/Y/Z layer turns;
- whole CoreCell stack relocation;
- projected board rotation;
- fusion/anchor recompute;
- reserve unaffected;
- four-turn roundtrip fixtures;
- SignalR and AI/search action smoke.

Deferred:
- full animation polish;
- online spectator UX;
- advanced layer-turn strategy/search.

## Slide 7 - Hodge Projection Duel

Hodge Projection Duel is separate from Asgard and Rubik.

Implemented:
- two macro-players;
- three projections per macro-player;
- all-or-nothing projected composite move;
- deterministic transform helpers;
- HPD action notation;
- online authority and AI/search smoke.

Deferred:
- final mathematical formalism;
- polished UI visualization;
- tournament-grade search.

## Slide 8 - Visual Asset Pipeline

Current visual asset system:
- canonical OBJ/MTL catalog;
- readable fallback materials;
- neutral background and lighting;
- shared Chess2D/Chess3D model packaging;
- disabled generated-piece manifest example;
- validation against absolute paths, private markers, and large generated binaries.

Future direction:
- GLB/glTF support once the WPF pipeline can load it safely;
- generated asset QA for scale, origin, orientation, material brightness, and licensing.

## Slide 9 - Online Authority

Implemented:
- `ChessOnlineServer` hosted SignalR prototype on Windows;
- authoritative table/session model;
- auth/session/persistence baseline;
- exact-profile matchmaking smoke;
- state-hash and action-log diagnostics;
- production package and Windows runbook.

Honest boundary:
- Linux-native rules authority is not proven yet;
- no public ranked matchmaking;
- no Redis/Azure SignalR/backplane;
- no complete anti-cheat claim.

## Slide 10 - Delivery And Next Steps

Ready today:
- Windows desktop apps;
- Windows hosted server prototype;
- portable `ProductionOutput`;
- CI artifact upload;
- reproducible contract tests.

Next:
- deployment decision package;
- Linux-native authority spike;
- richer visual presentation and online UX;
- final AI/search and matchmaking hardening.

