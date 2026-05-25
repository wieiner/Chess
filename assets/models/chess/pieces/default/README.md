# Default OBJ Chess Piece Set

This is the canonical local OBJ/MTL piece set used by Chess2D 3D-model mode
and Chess3D. The repo-level catalog is `assets/models/chess/pieces/piece_sets.json`.

Expected names:

Board/light_tile.obj
Board/dark_tile.obj
Pieces/white_pawn.obj
Pieces/white_knight.obj
Pieces/white_bishop.obj
Pieces/white_rook.obj
Pieces/white_queen.obj
Pieces/white_king.obj
Pieces/black_pawn.obj
Pieces/black_knight.obj
Pieces/black_bishop.obj
Pieces/black_rook.obj
Pieces/black_queen.obj
Pieces/black_king.obj

OBJ loader supports vertices (`v`), texture coordinates (`vt`), polygon faces
(`f`), and best-effort MTL material metadata:

- `map_Kd` diffuse textures are used when the referenced texture exists next to
  the model or under the set folder.
- `Ks` and `Ns` are used as best-effort specular hints.
- missing external textures, placeholder `map_Kd .`, normal maps, roughness maps,
  and PBR maps fall back to the procedural readable palette.
- black pieces must never fall back to pure black; the default fallback is a
  medium slate/charcoal material.
