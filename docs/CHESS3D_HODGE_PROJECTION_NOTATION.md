# Chess3D Hodge Projection Notation

P2J extends notation v0.1 with Hodge Projection Duel composite actions.

## Format

```text
#<index> M<macroPlayer> HPD primary=S<side> <piece> (from)->(to); mirrors=[S<side> <piece> (from)->(to), S<side> <piece> (from)->(to)]
```

Example:

```text
#1 M1 HPD primary=S1 P (3,3,0)->(3,3,1); mirrors=[S3 P (3,0,3)->(3,1,3), S5 P (0,3,3)->(1,3,3)]
```

The notation is deterministic and human-readable, but it is not final PGN. P2K or later should define replay/export/import syntax for all Chess3D action kinds.
