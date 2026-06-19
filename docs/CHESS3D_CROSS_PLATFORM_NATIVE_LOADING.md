# Chess3D Cross-Platform Native Loading

P4D phase 04 introduces a small C# native-loading boundary for Chess3D authority code. It does not change the native C ABI and it does not change any Chess3D rule profile semantics.

## Existing ABI remains stable

The managed wrapper still imports the existing logical library name:

```csharp
[DllImport("Chess3DEngine.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
```

The resolver maps that logical import to the platform-specific physical library name before .NET performs the native load.

## Platform names

| Platform | Physical library |
| --- | --- |
| Windows | `Chess3DEngine.dll` |
| Linux | `libChess3DEngine.so` |
| macOS future | `libChess3DEngine.dylib` |

macOS is documented only. P4D is focused on Windows compatibility and Linux-native server authority.

## Managed boundary

The resolver lives in `src/ChessApp/Chess3DNativeLibraryResolver.cs` because `NativeChess3DEngine.cs` is linked into both the WPF Chess3D app and `ChessOnlineProtocol`.

Responsibilities:

- register a `NativeLibrary.SetDllImportResolver` for the assembly that contains `NativeChess3DEngine`;
- keep the old logical import name stable;
- choose the expected platform library name;
- prefer `AppContext.BaseDirectory`;
- also check `runtimes/<rid>/native` for future publish layouts;
- expose diagnostics without requiring the native library to be loaded.

## Server diagnostics

`NativeChessOnlineGameSessionFactory.GetDiagnostics()` now reports:

- platform;
- OS description;
- process architecture;
- expected native library name;
- expected loaded path if present;
- whether the current platform is a supported native-authority platform.

On Windows the expected library remains `Chess3DEngine.dll`. On Linux the expected library is `libChess3DEngine.so`.

## What this phase does not do

- It does not build `libChess3DEngine.so`.
- It does not publish a linux-x64 server package.
- It does not change `net8.0-windows` target frameworks yet.
- It does not deploy anything to Hetzner.
- It does not change native DTO layout or function names.

Those steps belong to later P4D phases after the read-only Hetzner probe and Linux native build attempt.
