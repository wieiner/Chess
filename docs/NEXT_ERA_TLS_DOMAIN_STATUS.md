# Next Era TLS / Domain Status

Date: 2026-06-21

## Status

Phase 09 did not run Certbot and did not issue a TLS certificate.

The current Hetzner deployment has:

- `chessonline.service` running Kestrel on `127.0.0.1:5077`;
- Nginx proxying public HTTP port `80` to that loopback Kestrel service;
- external HTTP health, readiness, and diagnostics smoke passing;
- no configured real domain name in the repository or deployment template;
- no HTTPS listener, certificate, private key, or HTTP-to-HTTPS redirect.

The tracked Nginx config still uses:

```nginx
server_name _;
```

The tracked HTTPS snippet is intentionally a placeholder using `chess.example.invalid`. It is not a live domain and must not be used for certificate issuance.

## Why TLS Is Blocked

Let's Encrypt certificates require proving control of a domain name. The current public endpoint is reachable by IP address only, and no confirmed DNS name was provided for this phase.

Because there is no confirmed domain:

- Certbot was not installed or executed;
- no ACME challenge was attempted;
- no certificate or private key was generated;
- `RequireHttpsForTokens` was not enabled for public traffic;
- public account/token use remains unsafe.

## Current Security Boundary

Public HTTP is diagnostic-only.

Allowed over current public HTTP:

- `/healthz/live`;
- `/healthz/ready`;
- `/chess3d/diagnostics`;
- short-lived development smoke traffic using disposable accounts only.

Not allowed for real use until TLS exists:

- real user accounts;
- durable or reused passwords;
- long-lived refresh sessions;
- production tokens;
- public ranked matchmaking;
- any public identity workflow treated as secure.

## Future P4E TLS Steps

When a real domain points to the Hetzner host and the user confirms it, P4E can:

1. replace `server_name _` with the real domain;
2. install Certbot and the Nginx integration appropriate for the VPS OS;
3. issue a certificate for the domain;
4. configure HTTP to HTTPS redirect;
5. enable `RequireHttpsForTokens=true`;
6. verify `/healthz/live`, `/healthz/ready`, `/chess3d/diagnostics`, and SignalR over HTTPS;
7. document renewal and rollback.

No secrets, certificates, private keys, or runtime keyrings should be committed.

## References

- Microsoft Learn, Host ASP.NET Core on Linux with Nginx: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx
- Let's Encrypt, How It Works: https://letsencrypt.org/how-it-works/
- Certbot, Nginx instructions: https://certbot.eff.org/instructions?ws=nginx
