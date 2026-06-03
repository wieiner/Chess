# Chess3D Quiescence-Lite

P3D.1 adds bounded quiescence-lite to reduce obvious tactical horizon issues.

Scope:

- normal capture moves are considered tactical;
- reserve restore can be considered tactical when it directly restores material;
- Hodge projected composites are not expanded in quiescence in this pass;
- Rubik layer turns are not recursively explored in quiescence.

Limits:

- default max quiescence depth is `2`;
- qnodes are counted separately;
- node/time limits are still respected;
- no unbounded recursion is allowed.

This is not full chess quiescence search. It is a small profile-aware tactical extension over the existing legal action surface.

