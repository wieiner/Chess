# Chess3D P4B Deployment / Matchmaking / Asgard Audit

P4B starts from the P4A server baseline: SignalR authority, authenticated sessions, JSON persistence, profile-aware action validation, and portable `ProductionOutput` packaging are already present.

Runtime findings:
- ChessOnlineServer is still a Windows-targeted .NET server (`net8.0-windows`) because it relies on the native Chess3DEngine DLL.
- SignalR hub methods already support room/table/seat/ready/start/action/snapshot/action-log flows.
- P4A authentication can require bearer tokens and rejects spoofed player ids.
- Exactly five Chess3D RuleProfiles exist; scenario JSON files are not game modes.
- Asgard can already start through the authoritative table path and accepts profile-legal actions.

Safe P4B additions:
- Add single-server in-memory matchmaking over existing room/table authority.
- Add authenticated queue operations and match-found events.
- Add deployment templates and production sample config without secrets.
- Package matchmaking/asgard/deployment scenario descriptors.

Deferred:
- Linux-native runtime execution until the server/native engine boundary is portable.
- Public ranked matchmaking, Redis, cloud backplanes, Kubernetes, real SSH deploy, and online protocol changes for full production operations.
