# P4M Blender Conversion Adapter

`Convert-ModelAssetWithBlender.ps1` is an optional offline adapter for approved
Blend, FBX, OBJ, GLB, and glTF inputs. It does not install Blender and never
runs automatically in CI.

The adapter uses Blender background/factory mode with automatic script
execution disabled, then invokes the repository-owned conversion script
explicitly. It normalizes deterministic mesh names, optionally applies
rotation/scale, triangulates, disables animation/camera/light export, and emits
GLB or compatibility OBJ. A C# process watchdog bounds the conversion.

GLB is the preferred output because Blender packages mesh and image resources
into one binary container. The report records input/output hashes, Blender
version, command shape, mesh/triangle/material/texture counts, and warnings.
Machine-local paths stay in ignored logs and are not written into the report.

When Blender is unavailable, `-DryRun` returns `SKIPPED` and explains that no
installation was attempted. Generated output must still pass Phase 31
validation before it can enter a runtime manifest.

Official reference:
[Blender glTF 2.0 manual](https://docs.blender.org/manual/en/4.2/addons/import_export/scene_gltf2.html).
