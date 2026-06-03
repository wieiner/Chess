# Chess3D Hosted Server Lifecycle

## Development Run

Build `Release|x64`, then run:

```powershell
src\ChessOnlineServer\bin\x64\Release\net8.0-windows\ChessOnlineServer.exe --urls http://127.0.0.1:5077
```

The default hub URL is:

```text
http://127.0.0.1:5077/chess3d/relay
```

## Health Checks

- `/healthz/live`: process is alive.
- `/healthz/ready`: server options and profile root are available.
- `/chess3d/diagnostics`: local counters and configuration summary.

## Tests

`ChessOnlineSignalRContractTests` starts Kestrel in-process on a dynamic local port and stops it at the end of the suite. No orphan server process should remain after tests.

## Packaging

`Build-Production.ps1 -Product All` includes `ProductionOutput\ChessOnlineServer` and a root launcher `run_chess_online_server.bat`.

