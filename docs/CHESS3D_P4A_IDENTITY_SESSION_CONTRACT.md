# Chess3D P4A Identity Session Contract

P4A adds a production-oriented local identity/session layer around the P3E/P3F online authority. The authority registry remains responsible for rules, turns, legality, state hash, replay, and action acceptance.

## Implemented

- Persistent local player account store.
- Password hashing via ASP.NET Core `PasswordHasher`.
- Durable session records.
- Data Protection protected access/refresh tokens.
- Authenticated SignalR player identity.
- Player identity derived from authenticated session in production-like mode.
- Local JSON persistence provider for accounts, sessions, rooms, tables, seats, and accepted action logs.
- Explicit development fallback for anonymous P3F smoke flow.

## Not Implemented

- Public matchmaking.
- Cloud deployment.
- OAuth or external identity providers.
- Real email confirmation.
- Admin dashboard.
- Redis/Azure SignalR scale-out.
- Full anti-cheat claims.
- Binary protocol.
- Mobile client.

## Compatibility Rules

Existing P3E/P3F DTOs remain backward-compatible. Development anonymous sessions are available only when `HostedOnline.Auth.AllowDevAnonymousSessions=true`. Production-like tests set `EnableAuthentication=true` and `AllowDevAnonymousSessions=false`.

When authenticated, the hub rejects envelopes that claim a different `playerId` than the bearer session. The client is never the identity source of truth.
