# ChessOnlineServer Production Configuration

Use `src/ChessOnlineServer/appsettings.Production.sample.json` as a template. Copy it to an untracked deployment-local configuration file.

Do not commit:
- Access tokens.
- Refresh tokens.
- Passwords.
- DataProtection key rings.
- JSON stores or database files.
- TLS private keys or certificates.

Important settings:
- `HostUrls`: local Kestrel binding, normally behind nginx.
- `ProfileRoot`: packaged `Assets/Rules3D/Profiles`.
- `Auth.EnableAuthentication`: should be true for shared servers.
- `Auth.AllowDevAnonymousSessions`: false outside local development.
- `Persistence.StorePath`: runtime data path, not tracked.
- `DataProtection.KeyRingPath`: runtime key path, not tracked.
