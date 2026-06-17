# P4B Regression Fixtures

New descriptor groups:

- `assets/rules/scenarios/chess3d/matchmaking`
- `assets/rules/scenarios/chess3d/asgard_online`
- `assets/rules/scenarios/chess3d/deployment`

They are parsed by `ChessOnlineSignalRContractTests` and copied into OnlineServer output and ProductionOutput. The executable behavior is covered by SignalR tests rather than a cloud deployment runner.
