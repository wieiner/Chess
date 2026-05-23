# Chess3D Mode Selector UI

The Rule Profile selector is intentionally data-driven. It reads profile JSON files from `Assets/Rules3D/Profiles`, extracts `rulesetId` and `displayName`, and calls `Chess3D_LoadRuleProfileJson`.

The selector does not use `rude-resource/` and does not hardcode absolute paths. In development it falls back to the repository `assets/rules/profiles` folder only when the runtime output folder is not present.

The profile summary shows:

- ruleset id/display name;
- goal profile;
- capture profile;
- occupancy profile;
- fusion profile;
- core physics profile;
- layer turn profile;
- victory profile;
- projection state;
- last profile error.

