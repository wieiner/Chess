# Deployment Observability

P4B keeps observability minimal:

- `/healthz/live`
- `/healthz/ready`
- `/chess3d/diagnostics`
- SignalR contract test diagnostics checks

Diagnostics must not expose passwords, tokens, refresh tokens, key-ring material, or session secrets. Future production work should add structured logs and metrics.
