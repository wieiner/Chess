# Chess3D Action Log UI

P2K exposes the existing Chess3D action-history ABI in the app.

The UI shows the latest actions as deterministic notation strings. It can:

- refresh the visible list;
- copy the log to the clipboard;
- save a `.ch3dlog` text file.

The saved file starts with:

```text
rulesetId: <current ruleset id>
```

Then each action notation line follows. P2K does not implement import/replay; that is planned for P2L.

