# Chess3D Rule Profile Schema

The machine-readable schema lives at:

```text
assets/rules/profiles/chess3d_rule_profile.schema.json
```

The local contract tests also perform manual validation, because the repository baseline must not require an external JSON-schema validator.

P2D also uses the same profile shape at runtime through `Chess3D_LoadRuleProfileJson`. P2F reads the fusion and implosion profile fields needed for runtime descriptors. P2G reads knockback/reserve profile types needed for runtime capture routing. P2H reads layer-turn profile fields needed for Rubik convergence ritual turns. P2J reads projection profile fields needed for Hodge Projection Duel composite turns. The runtime parser is intentionally narrower than a full JSON-schema implementation: it reads known fields, ignores unknown optional metadata, and fails cleanly when required profile fields are missing or unsupported.

Minimum validation:

- `rulesetId` is present and non-empty;
- `boardProfile.width`, `boardProfile.height`, and `boardProfile.depth` are all `8`;
- `goalProfile.type` is one of `classicCheckmate`, `centerAssembly`, `hybrid`, `sandbox`, `centerAssemblyTraining`;
- `captureProfile.type` is one of `classicCapture`, `knockbackCapture`;
- `knockbackProfile.type` is one of `none`, `homeOrReserve`;
- `reserveProfile.type` is one of `none`, `disabled`, `sidePieceTypeCounts`;
- `occupancyProfile.type` is one of `exclusive`, `coreStack`, `quantumCore`;
- `fusionProfile.type` is one of `none`, `anchorOnly`, `pairFusion`, `stackFusion`, `colorPermutation`, `volumeSurface216`;
- `corePhysicsProfile.type` is optional and can declare `asgardCorePhysics`;
- `fusionProfile.status` may be `runtimePartial` for P2F descriptor support;
- `implosionProfile.type` is optional and can be `none` or `centerCompletion`;
- `implosionProfile.mode` can be `progressState`;
- `status` fields can mark profile data as `specOnly`, `runtimePartial`, or future/draft when runtime mechanics are partial or not implemented yet;
- `layerTurnProfile.type` is one of `disabled`, `ritualTurn`, `globalEvent`, `sandbox`;
- P2H also recognizes layer-turn metadata such as `axes`, `layers`, `quarterTurns`, `actionCost`, `movesProjectedBoard`, `movesCoreStacks`, `recomputesFusion`, `recomputesAnchors`, and `reserveInteraction`;
- `projectionProfile.type` is one of `none`, `hodgeTriuneProjection`;
- P2J recognizes projection metadata such as `enabled`, `macroPlayerCount`, `projectionCountPerMacroPlayer`, `groups`, `mirrorPolicy`, `actionHistoryMode`, and `transformProfile`;
- if `coreProfile.coreCube` is present, all bounds are in `0..7` and min <= max;
- if `corePhysicsProfile.volumeSurface216Principle` is present, it must parse and can be disabled for future symbolic rules;
- profile JSON must parse structurally as JSON.
