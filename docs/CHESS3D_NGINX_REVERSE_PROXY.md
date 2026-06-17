# nginx Reverse Proxy

`deploy/linux/nginx-chessonline.conf.template` forwards:

- `/chess3d/relay` as a WebSocket-capable SignalR endpoint.
- all other traffic to the local ChessOnlineServer Kestrel process.

Keep `proxy_http_version 1.1`, `Upgrade`, and `Connection` headers for SignalR. TLS certificate paths belong in server-local config and must not be committed.
