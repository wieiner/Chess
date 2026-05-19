# Chess3D Rule Profile Schema

The machine-readable schema lives at:

```text
assets/rules/profiles/chess3d_rule_profile.schema.json
```

The local contract tests also perform manual validation, because the repository baseline must not require an external JSON-schema validator.

Minimum validation:

- `rulesetId` is present and non-empty;
- `boardProfile.width`, `boardProfile.height`, and `boardProfile.depth` are all `8`;
- `goalProfile.type` is one of `classicCheckmate`, `centerAssembly`, `hybrid`, `sandbox`, `centerAssemblyTraining`;
- `captureProfile.type` is one of `classicCapture`, `knockbackCapture`;
- `occupancyProfile.type` is one of `exclusive`, `coreStack`, `quantumCore`;
- `fusionProfile.type` is one of `none`, `anchorOnly`, `pairFusion`, `stackFusion`, `colorPermutation`, `volumeSurface216`;
- `corePhysicsProfile.type` is optional and can declare `asgardCorePhysics`;
- `status` fields can mark profile data as `specOnly` when runtime mechanics are not implemented yet;
- `layerTurnProfile.type` is one of `disabled`, `ritualTurn`, `globalEvent`, `sandbox`;
- if `coreProfile.coreCube` is present, all bounds are in `0..7` and min <= max;
- if `corePhysicsProfile.volumeSurface216Principle` is present, it must parse and can be disabled for future symbolic rules;
- profile JSON must parse structurally as JSON.
