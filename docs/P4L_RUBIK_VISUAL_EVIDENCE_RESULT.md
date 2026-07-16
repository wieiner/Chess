# P4L Rubik Visual Evidence Result

## Evidence mechanism

`RubikApp` now captures its own named viewport host with WPF
`RenderTargetBitmap` and encodes PNG files with `PngBitmapEncoder`. It does not
activate windows, send keyboard input, switch desktop focus, or capture the
whole screen.

The command can be invoked from the UI with **Save Visual Evidence** or from a
Release build:

```powershell
.\src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe --save-visual-evidence
```

Output is written under ignored `.tmp/rubik-visual-evidence`:

- `solved-3x3.png`;
- `turn-r-3x3.png`;
- `solved-11x11.png`;
- `inner-slice-11x11.png`;
- `scramble-11x11.png`;
- `manifest.json`.

The manifest records dimensions, cube size, cubie/sticker counts, invalid
stickers, and fallback status. It contains no credentials or machine secrets.

## Safety

Evidence generation accepts only a trusted non-manual state. It preserves the
current size, complete native history, facelets, surface mode, selection, and
camera. After capture, it reconstructs the original cube by replaying trusted
history and verifies exact facelet equality. A manual state clean-fails because
its cubie decomposition cannot be restored honestly.

The generated PNGs are visual evidence, while `RubikVisualContractTests`
remain the deterministic correctness authority.

## Recorded run

The Release x64 app was executed through `TestProcessWatchdog` with a 120 second
limit. It completed in 18 seconds with exit code 0 and produced five `916x648`
PNGs.

Measured manifest values:

- solved/turned 3x3: 26 shell cubies, 54 stickers;
- solved/inner/scrambled 11x11: 602 shell cubies, 726 stickers;
- every scene: zero invalid stickers, orientation available, fallback off.

Visual inspection confirmed a white/green/red three-color corner, two-color
edge cubies, dark plastic borders/gaps, and distinct sticker colors after the
3x3 R turn and the 11x11 scramble. The ignored output policy was confirmed with
`git check-ignore`.
