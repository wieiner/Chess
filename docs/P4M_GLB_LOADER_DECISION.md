# P4M GLB Runtime Loader Decision

## Decision

P4M approves **no third-party runtime loader package**. The repository will
implement a bounded, static-mesh-only GLB 2.0 reader behind its own immutable
runtime model. There is therefore no package/version or third-party runtime
license to add in Phase 34.

The supported production subset is:

- GLB 2.0 container with one JSON and optional BIN chunk;
- scenes/nodes and local/world transforms;
- static triangle primitives;
- indexed or non-indexed POSITION, NORMAL, and TEXCOORD_0 accessors;
- base-color factor and embedded PNG/JPEG base-color image;
- opaque/mask/blend metadata and double-sided metadata.

Required unknown extensions, skins, morph targets, animation, Draco, meshopt,
KTX/Basis and custom shader extensions are rejected or reported unsupported.
No external HTTP URI is fetched.

## Candidate comparison

| Candidate | Maintenance/license | Strengths | Mismatch/risk | Decision |
| --- | --- | --- | --- | --- |
| SharpGLTF Core/Runtime | Active public .NET repository; MIT | Mature glTF schema, read/write and runtime helpers; broad feature coverage | General object graph exposes more formats/resources/extensions than P4M needs; allocation and URI policy still need a defensive adapter | Reconsider if the subset grows beyond static assets |
| Unity glTFast | Active Unity package; Apache-2.0 | Efficient, broad extensions, platform focus | Unity object/shader/package dependency is incompatible with WPF | Reject |
| Khronos UnityGLTF | Active Khronos Unity package; MIT | Broad import/export and extension plugin model | Unity runtime, shaders and web loaders are outside this application | Reject |
| Assimp + .NET binding | Native Assimp active; BSD-3-Clause | Many model formats | Adds native deployment and a very broad parser; current AssimpNet package is old and FBX is not a runtime requirement | Keep offline conversion only, not runtime |
| Offline GLB-to-OBJ only | Existing OBJ loader and fallback | Lowest runtime work | Loses node/material/embedded texture semantics and makes GLB support nominal rather than real | Retain as emergency authoring workaround |
| Bounded repository parser | No dependency; exact contract | Closed allocation, URI, extension and diagnostic policy; direct immutable boundary | More internal tests and a deliberately small subset | **Approved for P4M** |

## Security and memory boundary

- maximum file, JSON chunk, buffer, accessor, vertex, index, image and node
  counts are checked before allocation;
- integer offset/stride/count arithmetic is checked;
- only GLB-contained buffers/images are accepted in the first release;
- required unsupported extensions fail;
- cancellation is checked throughout parsing;
- library/schema objects never reach WPF;
- cache identity is the validated SHA-256, not a path;
- malformed input returns diagnostics and triggers OBJ/procedural fallback.

## Upgrade policy

New glTF features require a fixture, bounded parser change, runtime-model
representation, WPF behavior, and explicit unsupported-feature migration. If
animation, skinning, compressed geometry, or broad extension support becomes a
product requirement, repeat this audit and prefer a pinned SharpGLTF version
over expanding a custom parser indefinitely.

Primary sources:

- [Khronos glTF 2.0 specification](https://github.com/KhronosGroup/glTF/tree/main/specification/2.0)
- [SharpGLTF repository](https://github.com/vpenades/SharpGLTF)
- [Unity glTFast repository](https://github.com/atteneder/glTFast)
- [Khronos UnityGLTF repository](https://github.com/KhronosGroup/UnityGLTF)
- [Assimp repository](https://github.com/assimp/assimp)
