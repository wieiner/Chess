# Chess3D Mode Capabilities Matrix

| Profile | Core Stack | Fusion | Reserve | Rubik Layer Turn | Hodge Projection | Main Purpose |
| --- | --- | --- | --- | --- | --- | --- |
| Classic Six-Side | No | No | No | No | No | Classic draft six-side 3D chess |
| Single-Side | No | No | No | No | No | Movement/setup training and tests |
| Asgard / Meru Convergence | Yes | Yes | Yes | No | No | Forbidden Core, anchors, reserve, fusion descriptors |
| Rubik Convergence | Yes | Yes | Yes | Yes | No | Asgard-style mode plus runtime layer turns |
| Hodge Projection Duel | No | No | No | No | Yes | Two macro-players with three projected sides each |

This separation is deliberate. Asgard, Rubik, and Hodge are not variants hidden inside one hardcoded rules branch; each is selected through a RuleProfile.

