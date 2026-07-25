# P4M Model Asset Preview

`ModelAssetPreview.exe` is an isolated WPF diagnostic surface for v2 manifests,
GLB, and compatibility OBJ. It does not modify runtime assets.

Features:

- open/reload manifest or model and run the managed validator;
- bounded GLB load or existing OBJ compatibility load;
- orbit/drag, wheel/button zoom and camera reset;
- optional bounds, origin, axes and ground overlays;
- studio/neutral/high-contrast lights and three backgrounds;
- set/role/format/SHA/license/units/bounds/counts/issues/unsupported diagnostics;
- JSON report export;
- in-process `RenderTargetBitmap` evidence plus descriptor JSON under ignored
  `.tmp/model-evidence`.

WPF Media3D has no native wireframe or normal-glyph mode. Those controls are
visible but disabled and named as deferred; the tool does not pretend that
they ran. Evidence is not a substitute for manifest validation.
