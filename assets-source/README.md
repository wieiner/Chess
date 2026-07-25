# Approved Asset Sources

`assets-source/models` is the tracked source boundary for assets whose author,
license, provenance, size, and repository storage have been reviewed.

Raw FBX, Blend, archives, purchased packages, high-poly meshes, and unreviewed
textures do not belong here. Put them in the ignored inbox instead:

```text
rude-resource/model-inbox/<category>/<set-id>/
```

After review and conversion:

- approved editable source goes to `assets-source/models/<category>/<set-id>/`;
- validated runtime GLB/OBJ/MTL/textures go to
  `assets/models/<category>/<set-id>/`.

Applications never load this source tree directly.
