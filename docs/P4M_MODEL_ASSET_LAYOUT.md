# P4M Model Asset Layout

## Boundaries

`assets-source/models` stores editable authoring inputs. It is never copied to product output and is never searched by an application at runtime. `assets/models` stores reviewed runtime assets, closed manifests, and small policy files. `.tmp/assets-import` is the disposable staging area for imports and conversions.

Raw FBX, Blend, ZIP, high-poly meshes, downloaded packages, and unreviewed
textures go to:

```text
rude-resource/model-inbox/<category>/<set-id>/
```

Create the ignored inbox with:

```powershell
pwsh -NoProfile -File .\scripts\assets\Initialize-ModelInbox.ps1
```

Approved editable sources go to
`assets-source/models/<category>/<set-id>/`. Validated runtime GLB, OBJ, MTL,
PNG, and JPEG files go to `assets/models/<category>/<set-id>/`.

## Namespace map

| Namespace | Purpose | Required fallback |
| --- | --- | --- |
| `chess/pieces` | Chess2D and common Chess3D piece sets | Procedural pieces |
| `chess/boards` | Optional boards and tiles | Procedural board |
| `chess3d/common` | Profile-neutral Chess3D visuals | Existing cell visuals |
| `chess3d/asgard` | Core, stack, fusion, reserve, anchor visuals | Existing overlays |
| `chess3d/rubik-convergence` | Convergence-only layer visuals | Existing overlays |
| `chess3d/hodge` | Projection/mirror visuals | Existing arrows/overlays |
| `rubik/cubies` | Standalone Rubik cubies | Procedural cubies |
| `rubik/stickers` | Standalone Rubik stickers | Procedural facelets |

## Promotion rule

A source asset reaches runtime only after license/provenance review, bounded conversion, hash generation, manifest validation, geometry/material QA, and package verification. Promotion copies an optimized derivative; it does not make the source directory a runtime dependency.

Runtime discovery is manifest-authoritative in v2. Directory names and loose files alone do not enable a set. Legacy v1 discovery remains supported until its adapter is retired.

## Repository hygiene

Do not track temporary exports, Blender caches, render previews, package archives, absolute workstation paths, secrets, or runtime reports. Large-source placement follows `P4M_GIT_LFS_POLICY.md`; adding Git LFS requires an explicit policy decision rather than an automatic attribute change.
