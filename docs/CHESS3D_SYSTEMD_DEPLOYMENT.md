# systemd Deployment

`deploy/linux/chessonline-server.service.template` describes the intended Linux service shape:

- dedicated `chess` user/group;
- working directory `/opt/chess-online-server`;
- Kestrel bound to `127.0.0.1:5077`;
- nginx handles public HTTP/WebSocket traffic;
- runtime data remains under the deployment directory and outside git.

This is a template only until Linux runtime portability is complete.
