# P4M Runtime Model Boundary

`RuntimeModelAsset` is the renderer-independent contract between bounded file
parsing and WPF. It represents node hierarchy and transforms, triangle
primitives, positions/normals/UV0, unsigned indices, materials, embedded
textures, bounds, warnings, and explicitly unsupported features.

Constructed models defensively copy top-level collections and use a validated
SHA-256 as identity. Library/parser objects never enter ChessApp or
Chess3DApp.

`RuntimeModelLoadLimits` bounds file/JSON/buffer/image sizes and all major
counts. Resource policy forbids absolute/network URIs and traversal. Checked
range arithmetic executes before allocation or accessor reads. Loaders accept
a cancellation token and must return an all-or-nothing immutable result.

Phase 34 supplies the GLB implementation; Phase 35 performs the short
UI-thread WPF conversion/assignment and caches frozen WPF resources by content
hash.
