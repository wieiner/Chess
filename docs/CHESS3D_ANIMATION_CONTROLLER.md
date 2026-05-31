# Chess3D Animation Controller

P3C keeps animations short and useful. The WPF layer uses an animation lock around transient visual effects so player input cannot double-apply an action.

## Contract

- Begin animation: lock input and render transient overlays.
- End animation: clear transient overlays and unlock input.
- Exceptions must cleanly unlock input.
- Engine actions are invoked once.
- Profile load, reset, replay import, and clear selection remove transient visuals.

## Current Effects

- Move/action path flash.
- Replay step path flash where coordinates are available.
- Rubik layer pre-highlight.
- Hodge primary/mirror/blocked arrow highlight.
- Check marker through cell overlay.

This is not a cinematic engine. It is a playability layer.
