# P4M Bounded GLB Runtime

`GlbRuntimeModelLoader` implements the approved static GLB 2.0 subset without a
new package. It validates content SHA before parsing; bounds container, JSON,
BIN, view, accessor, image and topology sizes; uses checked range arithmetic;
and checks cancellation during loops.

Supported:

- one embedded GLB buffer;
- hierarchy and matrix/TRS transforms;
- static TRIANGLES primitives;
- float POSITION/NORMAL/TEXCOORD_0;
- unsigned byte/short/int indices or generated sequential indices;
- base-color factor and embedded PNG/JPEG base-color texture;
- opaque/mask/blend metadata and double-sided materials.

Required unknown extensions fail. Optional extensions, skins, animations and
morph targets are reported as unsupported diagnostics. Sparse accessors,
external/data URIs, non-triangle modes, Draco, meshopt, KTX/Basis and custom
shader execution are rejected or not loaded. Malformed ranges, indices,
counts, NaN/Infinity, transforms and hierarchy fail transactionally before a
runtime model is returned.

Contract fixtures generate their tiny GLB bytes at test time; no unreviewed
binary sample is tracked.
