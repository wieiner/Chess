# Chess3D P4A Security Limitations

P4A is a local production-oriented foundation. It is not a public production launch.

Implemented safeguards:

- no plaintext passwords;
- refresh tokens are stored as hashes;
- access/refresh payloads are protected with ASP.NET Data Protection;
- authenticated hub identity is server-derived;
- diagnostics avoid tokens and password hashes;
- verify rejects runtime database/key/certificate/token artifacts in `ProductionOutput`.

Limitations:

- local JSON persistence is not a cloud database;
- Data Protection keys are not encrypted by default on all developer machines;
- no OAuth, MFA, email confirmation, or account recovery;
- no public matchmaking or moderation;
- no full anti-cheat;
- no Redis/Azure SignalR backplane;
- no claims about WAN-scale production hardening.

Operators must configure durable storage, key-ring protection, HTTPS, logging, and account operations before public deployment.
