# Chess Visual Theme And Lighting

P2M makes pieces readable before adding richer animation.

## Material Palette

- White pieces use warm ivory (`#EEE7CE`) instead of flat pure white.
- Black pieces use medium slate/charcoal (`#58606C`) instead of pure black.
- Pure black is avoided because it collapses under WPF diffuse lighting and disappears on dark boards/backgrounds.

## Lighting

Chess2D 3D-model mode and Chess3D now use:

- stronger ambient fill;
- warm key directional light;
- cool rim/fill directional light;
- specular highlights through `MaterialGroup`.

## Chess3D Background

The Chess3D preview host uses a neutral gray background instead of near-black. This preserves contrast for dark pieces while keeping the board readable.

## Where To Change

- Shared material/color behavior: `src/ChessApp/ObjModelLibrary.cs`
- Chess2D 3D scene lighting: `src/ChessApp/MainWindow.xaml.cs`
- Chess3D scene lighting/background/status: `src/ChessApp/Chess3DWindow.xaml(.cs)`
