# Linux VPS Runbook

P4B runbook status: planning scaffold.

Intended steps after Linux portability is solved:
1. build/publish ChessOnlineServer for Linux;
2. copy package to `/opt/chess-online-server`;
3. create a dedicated user;
4. place local `appsettings.Production.json`;
5. configure systemd service;
6. configure nginx reverse proxy;
7. configure TLS outside git;
8. validate `/healthz/live`, `/healthz/ready`, and `/chess3d/diagnostics`.

Do not use this as a live Linux runtime claim until the native engine boundary is portable.
