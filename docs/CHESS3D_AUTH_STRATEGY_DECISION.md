# Chess3D Auth Strategy Decision

P4A considered three implementation strategies.

## A. ASP.NET Core Identity Core + EF Core SQLite

Pros: mature account model, lockout patterns, token infrastructure, EF migrations.

Cons: heavier dependency and migration surface for a local WPF/SignalR prototype; more account features than P4A needs.

## B. Custom Store + PasswordHasher + Data Protection

Pros: small surface, good fit for local hosted SignalR, explicit session records, easy packaging, no plaintext passwords.

Cons: the project owns store schema, lockout rules, and token revocation logic.

## C. JWT Bearer Tokens

Pros: standard bearer model and easy interop.

Cons: key rotation, revocation, and refresh-token handling add production complexity that P4A is not ready to claim.

## Decision

P4A uses option B:

- `ChessOnlinePersistence` provides provider-style interfaces and a JSON baseline store.
- `PasswordHasher<PlayerAccountEntity>` stores password hashes.
- ASP.NET Data Protection protects access and refresh token payloads.
- The JSON store keeps only a refresh token hash, never the raw token.

This is production-oriented foundation, not a public production launch.
