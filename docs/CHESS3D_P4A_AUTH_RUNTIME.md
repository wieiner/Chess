# Chess3D P4A Auth Runtime

The hosted server exposes local JSON HTTP endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

Register/login return an access token, refresh token, player id, session id, display name, and expiry. Diagnostics never include tokens or password hashes.

SignalR clients pass the access token as bearer authorization or `access_token` query value. In authenticated mode the hub derives `playerId` and `sessionId` from claims and overwrites the message envelope before calling the P3E registry.

Development compatibility:

```json
{
  "HostedOnline": {
    "Auth": {
      "EnableAuthentication": false,
      "AllowDevAnonymousSessions": true
    }
  }
}
```

Production-like local mode:

```json
{
  "HostedOnline": {
    "Auth": {
      "EnableAuthentication": true,
      "AllowDevAnonymousSessions": false
    }
  }
}
```

P4A does not add OAuth, email confirmation, or public account recovery.
