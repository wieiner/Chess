# P4L Rubik Cubie Orientation

## Model

RubikEngine now tracks a discrete orientation basis for every current cubie
position. The basis contains three signed unit vectors:

- the cubie's solved/local X axis expressed in world coordinates;
- the cubie's solved/local Y axis expressed in world coordinates;
- the cubie's solved/local Z axis expressed in world coordinates.

The solved basis is the exact integer identity. A layer turn applies the same
signed axis transform used for facelet normals. No floating point values,
quaternions, normalization, or epsilon comparisons are involved, so four
quarter turns return bit-exactly to identity.

Orientation storage is indexed by current board position, in parallel with the
existing cubie-ID vector. During a turn, the basis moves to the destination
position and is rotated in world space. Cubie IDs, orientation bases, and
facelets are all computed before their successful logical commit.

## Sticker mask

`Rubik_GetCubieStickerMask` describes the physical faces present on the cubie
at a queried current position. It derives the cubie's solved coordinate from
the existing cubie ID and returns these stable local-face bits:

| Bit | Face |
| --- | --- |
| `1 << 0` | U |
| `1 << 1` | R |
| `1 << 2` | F |
| `1 << 3` | D |
| `1 << 4` | L |
| `1 << 5` | B |

The bit count classifies the physical part:

- 3: corner;
- 2: edge or wing;
- 1: center;
- 0: internal cubie.

The mask follows cubie identity as it moves. Combining its local face bits with
the orientation basis tells a renderer which world surfaces carry stickers.

## Append-only debug ABI

```cpp
struct RubikCubieOrientationDto
{
    int localXWorldX, localXWorldY, localXWorldZ;
    int localYWorldX, localYWorldY, localYWorldZ;
    int localZWorldX, localZWorldY, localZWorldZ;
};

int Rubik_GetCubieOrientation(
    void* handle,
    int x,
    int y,
    int z,
    RubikCubieOrientationDto* orientation);

int Rubik_GetCubieStickerMask(void* handle, int x, int y, int z);
```

The old `RubikStateDto` and all previous exports are unchanged. `RubikApp`
exposes matching managed wrappers and a sequential-layout DTO.

## Imported-state boundary

Reset, scramble, and legal layer turns keep cubie orientation synchronized.
Legacy integer edits cannot provide orientation and therefore invalidate the
orientation diagnostic.

A facelet-only import also does not invent cubie identities or orientation.
It remains valid solver/state input, but `Rubik_GetCubieOrientation` returns 0
until a later physical decomposition/solvability stage can prove a mapping.
`Rubik_GetLastInfo` reports this boundary. Sticker colors themselves remain
available through the independent facelet API.

## Verification

Contract tests verify:

- corner, edge, center, and internal sticker-mask counts;
- identity basis on reset;
- exact basis vectors after a Z quarter turn;
- cubie mask movement to the destination position;
- exact identity after inverse and four-turn cycles on X, Y, and Z;
- no fabricated orientation after facelet-only import;
- managed P/Invoke compilation in `RubikApp`.
