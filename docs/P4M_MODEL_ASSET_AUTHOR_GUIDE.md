# P4M Model Asset Author Guide

## Start a local set

```powershell
pwsh -NoProfile -File .\scripts\assets\Initialize-ModelInbox.ps1
pwsh -NoProfile -File .\scripts\assets\New-ModelSet.ps1 `
  -SetId my-chess-set `
  -Template ordinary-chess
```

Raw FBX and Blend files go to:

```text
rude-resource/model-inbox/<category>/<set-id>/source/
```

Source textures go to `textures/`; license text and author/source evidence go
to `license/`. Fill the local metadata draft before conversion/import. The
entire inbox is ignored and never packaged.

Available templates:

- `ordinary-chess`: twelve color/piece roles plus optional board roles;
- `chess3d-common`: six profile-neutral piece roles;
- `asgard`: core, anchor, reserve and fusion markers;
- `rubik-convergence`: core/layer/turn markers;
- `hodge`: primary/mirror/projection markers;
- `rubik`: optional cubie body, sticker and core.

Models are visual resources only. Role templates do not add rule profiles,
change mechanics, or replace authoritative Rubik facelets.

## Authoring requirements

- use stable object/material names;
- record real author, source URL/provenance, license and source SHA;
- apply intentional units, coordinate system, scale and pivot;
- keep source texture references relative;
- avoid scripts, linked private paths, caches and generated previews;
- triangulate and generate normals intentionally;
- use PBR base color in Blender, knowing WPF currently consumes only the
  supported GLB base-color subset;
- optimize runtime geometry rather than committing a high-poly source export.

No model may be enabled until validation, preview, license review and package
QA pass.
