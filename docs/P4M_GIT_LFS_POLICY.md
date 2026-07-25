# P4M Git LFS and Large Model Asset Policy

## Current inventory

Inventory date: 2026-07-25. Values are working-tree bytes; no compression claim
is made for formats that are already compressed.

| Path/class | Extension | Files | Bytes | Largest | Classification | Churn | CI/release |
| --- | --- | ---: | ---: | ---: | --- | --- | --- |
| Legacy piece meshes | `.obj` | 14 | 25,581,882 | 2,598,551 | `normalGit` (legacy) | low | required |
| Legacy materials | `.mtl` | 14 | 3,792 | 340 | `normalGit` | low | required |
| Manifests/schema | `.json` | 3 | 11,346 | 5,182 | `normalGit` | low | required |
| Asset policy markers | `.md` | 18 | 3,638 | 1,078 | `normalGit` | low | not packaged unless licensed notice |
| GLB/textures/source formats | mixed | 0 | 0 | 0 | not yet applicable | unknown | not yet required |

The legacy OBJ files are already in normal Git history and each remains below
3 MiB. Migrating them to LFS now would rewrite workflow without reducing
existing history, so it is not approved in P4M.

## Storage decisions

| Category | Typical use | Storage decision |
| --- | --- | --- |
| `normalGit` | Stable manifests, licenses, scripts, small textures, optimized runtime files normally below 5 MiB | Track normally after validation. |
| `gitLfsCandidate` | Approved binary source/runtime asset above 5 MiB, or a lower-size binary expected to change repeatedly | Stop and obtain an explicit repository/LFS decision before adding attributes or content. |
| `ignoredLocalSource` | Raw Blend/FBX, high-poly meshes, purchased packages, unreviewed textures | Keep under `rude-resource/model-inbox`; do not track. |
| `externalArchive` | Approved source archive above 50 MiB or vendor package whose contents are not release inputs | Keep in an approved external store with SHA/provenance metadata. |
| `generatedAtBuild` | Deterministic report, cache, preview image, package archive | Generate under `.tmp`/build output and do not track. |
| `prohibited` | Secret, private path, incompatible/unresolved license, unknown author/source, executable payload in an asset package | Reject. |

The 5 MiB value is a review trigger, not a universal GitHub limit. File type,
compressibility, expected churn, release necessity, offline workflow, and
license determine the final class. GitHub recommends LFS for binary files and
enforces a 100 MiB regular-Git object limit, but healthy repository operation
needs a stricter local decision.

## Format guidance

- `.blend`, `.fbx`, PSD/Krita source and high-resolution source textures:
  `ignoredLocalSource` until approved; then decide LFS versus external archive.
- `.glb`: prefer optimized runtime derivatives. Below 5 MiB and stable may use
  normal Git; larger or frequently replaced files are LFS candidates.
- `.obj`/`.mtl`: compatibility runtime only. Use normal Git when optimized and
  small; avoid duplicated per-color geometry.
- PNG/JPEG: reviewed runtime textures may use normal Git when small; high
  resolution or layered sources are not runtime assets.
- ZIP/7z/RAR: never runtime inputs and ignored by repository policy.
- Reports, screenshots, validator caches: generated, ignored.

## Clone, CI, archive, and fork effects

Git LFS stores pointer files in Git and downloads content separately. A clone
without LFS or an exhausted account can contain only pointers, which would
break an offline build if CI/release assets were moved without bootstrap
checks. GitHub-generated source archives omit LFS objects by default unless a
repository administrator enables inclusion; archive downloads can consume LFS
bandwidth. Fork and contributor workflows therefore require an explicit
rollout plan and package assertions before LFS is enabled.

The current CI and release remain self-contained with normal Git. `.gitattributes`
is intentionally unchanged in this phase.

## Approval checklist

Before enabling LFS for any pattern:

1. Record author, license, source URL/provenance, SHA-256, size, churn, and role.
2. Confirm the asset is required in source, CI, and/or release.
3. Estimate storage and recurring clone/archive bandwidth.
4. Define behavior for contributors without LFS and GitHub source archives.
5. Add a package check that rejects pointer text in place of required content.
6. Obtain an explicit user decision, then change `.gitattributes` in its own
   reviewed commit.

Official references:

- [GitHub: About Git Large File Storage](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage)
- [GitHub: Repository limits](https://docs.github.com/en/repositories/creating-and-managing-repositories/repository-limits)
- [GitHub: Git LFS billing](https://docs.github.com/en/billing/using-the-new-billing-platform/about-billing-for-git-large-file-storage)
- [GitHub: LFS objects in source archives](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/managing-git-lfs-objects-in-archives-of-your-repository)
