# P4M WPF Runtime Model Rendering

`ModelAssets.Wpf` converts the renderer-independent runtime model into
`Model3DGroup`, `GeometryModel3D`, `MeshGeometry3D`, `MaterialGroup`,
`ImageBrush`, and one `MatrixTransform3D` per rendered node.

Meshes, brushes, materials, transforms, node groups and complete models are
frozen when possible. Hash/index keys reuse immutable meshes, textures,
materials and complete model graphs. `BackMaterial` is assigned only for
double-sided materials. File parsing remains outside this WPF library; the
Dispatcher only needs to assign the final frozen model.

`ModelFormatResolver` reports the selected path and reason:

```text
validated GLB
-> validated OBJ (`objFallback`)
-> procedural (`proceduralFallback`)
```

Missing, invalid and unsupported GLB reasons stay distinguishable. Texture
decode failure falls back to base-color material and emits a warning.

The implementation follows
[Microsoft WPF 3D performance guidance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/maximize-wpf-3d-performance):
reuse/freeze resources, use indexed meshes, avoid unnecessary `BackMaterial`,
pre-size collections, and avoid per-frame mesh rebuilds.
