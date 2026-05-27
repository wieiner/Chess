# Chess3D Visual Performance Notes

P3B deliberately avoids a heavy rendering rewrite.

## Current Strategy

- Rebuild the viewport after committed actions.
- Keep hover/selection logic lightweight.
- Use simple cube overlays and dotted path markers.
- Reuse OBJ mesh/material caches from `ObjModelLibrary`.
- Report model, fallback, overlay, and animation counts in visual diagnostics.

## Deferred

Instancing, retained per-cell visual caches, true line shaders, animated mesh transforms, and screenshot automation can come later if profiling shows they are needed.
