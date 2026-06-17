# Chess3D P4C Project Platform Matrix

Phase: P4C Phase 01

| Project | Purpose | Target / type | Platform dependency | Uses native DLL? | Uses WPF? | Linux runtime today? | Windows package today? | Test coverage |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `src/ChessEngine` | Classic 2D chess rules/search native engine | C++ DLL | Windows MSBuild output today | n/a | no | not proven | yes | `ChessEngineContractTests`, benchmark |
| `src/Chess3DEngine` | Chess3D rules, profiles, stacks, fusion, Rubik, Hodge, AI/search, save/replay | C++ DLL | Windows native DLL today | n/a | no | blocked: no Linux `.so` artifact | yes | `Chess3DEngineContractTests`, online tests through server |
| `src/RubikEngine` | Standalone Rubik engine | C++ DLL | Windows native DLL today | n/a | no | not proven | yes | `RubikEngineContractTests` |
| `src/ChessGpuBackend` | Direct3D/GPU backend | C++ DLL | Windows graphics stack | n/a | no | no | yes | `GpuBackendContractTests` |
| `src/ChessCudaBackend` | Optional CUDA backend | C++/CUDA DLL | optional CUDA toolkit | n/a | no | optional/not required | optional | benchmark reports unavailable safely |
| `src/Chess2DBenchmark` | Native benchmark executable | C++ exe | Windows build today | yes, native engines | no | not proven | yes | quick benchmark in verify |
| `src/ChessApp` | Chess2D WPF app and shared Chess3D window/code | `net8.0-windows` | Windows desktop/WPF | yes: `ChessEngine.dll`, `Chess3DEngine.dll`, GPU | yes | no | yes | build + shared contract coverage |
| `src/Chess3DApp` | Chess3D WPF launcher/app | `net8.0-windows` | Windows desktop/WPF | yes via linked ChessApp code | yes | no | yes | build + native contract tests |
| `src/RubikApp` | Standalone Rubik WPF app | `net8.0-windows` | Windows desktop/WPF | yes: `RubikEngine.dll` | yes | no | yes | build + Rubik contract tests |
| `src/ChessOnlineApp` | WPF control/online integration app | `net8.0-windows` | Windows desktop/WPF | yes: copied `Chess3DEngine.dll` | yes | no | yes | build + SignalR tests indirectly |
| `src/ChessOnlineProtocol` | Managed online DTO/session/registry protocol layer | `net8.0-windows` | Windows target today though no WPF | yes indirectly: `OnlineGameSession` uses engine wrapper | no | blocked by TFM + native dependency | yes | `ChessOnlineContractTests`, SignalR tests |
| `src/ChessOnlinePersistence` | Managed identity/session/room persistence | `net8.0-windows` | Windows target today though portable-shaped code | no direct native dependency | no | blocked by TFM only, likely portable-ready after retarget audit | yes | SignalR auth/persistence tests |
| `src/ChessOnlineServer` | ASP.NET Core hosted SignalR server | `net8.0-windows` | Windows target today | yes: copies `Chess3DEngine.dll` | no | blocked by TFM + native rules authority | yes | `ChessOnlineSignalRContractTests` |

## Immediate Conclusions

- The executable Linux blocker is server authority, not SignalR itself.
- WPF applications should remain Windows products.
- `ChessOnlinePersistence` appears easiest to retarget later because it has no obvious WPF/native dependency.
- `ChessOnlineProtocol` contains both pure DTOs and authoritative game/session logic; it is not purely portable while `OnlineGameSession` owns a `Chess3DEngine` instance.
- A future P4C/P4D adapter boundary should separate transport/auth/persistence from native rules authority.
