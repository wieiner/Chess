# P4F Secret and Logging Audit

Date: 2026-06-27

## Scope

This audit covers the P4F playable online client MVP, shared online client SDK, Hetzner smoke tooling, and operator documentation.

P4F stays on diagnostic/dev HTTP 80. TLS/domain/443 are deferred and no server firewall, Nginx, systemd, x-ui, Xray, Outline, Unreal, or Albatronix state is changed by this phase.

## Search

The audit searched tracked source, tests, tools, scripts, and docs for:

```text
accessToken refreshToken password Authorization Bearer 178.105.220.117 id_ed25519 privateKey shortId uuid x-ui xray
```

## Findings

No committed access token, refresh token, password value, private key, certificate, runtime store, keyring, or raw SSH log was found.

The matches are expected categories:

- DTO/property names such as `AccessToken`, `RefreshToken`, and `Authorization`.
- server-side token creation and password-hash code.
- tests that assert diagnostics do not expose credential fields.
- token redaction tests with synthetic strings.
- temporary password variables generated at runtime in smoke/client code.
- documentation warnings that HTTP 80 is diagnostic-only.
- the Hetzner SSH key path placeholder/command shape, never private key contents.
- `x-ui`/`Xray` references only as out-of-scope service warnings.
- the public Hetzner HTTP host in operator commands, the baseline report, smoke script default, and the `Use Hetzner HTTP` UI preset.

## P4F Client Handling

`ChessOnlineClientSession` holds auth responses in memory only.

`ChessOnlineSecretRedactor` redacts:

- access-token fragments;
- refresh-token fragments;
- password fragments;
- `Authorization: Bearer ...`;
- standalone bearer strings.

`ChessOnlineClientEventLog` redacts entries before storing them in the UI event log.

`ChessOnlineApp` generated temporary users use random suffixes. Generated passwords are not displayed, not logged, and not written to session reports. Manual login is still available, but the UI warns that HTTP 80 is diagnostic/dev only and must not be used with real passwords.

## Runtime Reports and Logs

P4F session reports are written under:

```text
.tmp/manual-smoke
```

Test and smoke logs are written under:

```text
.tmp/test-logs
```

`.gitignore` already ignores `.tmp/`, `DeploymentOutput/`, and `ProductionOutput/`.

## Real IP Boundary

The current public Hetzner HTTP host appears in tracked P4F operator commands and in the `Use Hetzner HTTP` preset. It is not a secret, but it remains a diagnostic/dev endpoint. Do not enter real credentials until HTTPS/TLS and production auth policy are complete.

For reusable examples and public-facing documentation, prefer:

```text
http://<HETZNER_HOST>
```

## Result

PASS for P4F:

- no credentials committed;
- no tokens printed by the smoke path;
- no tokens/passwords written to session reports;
- no runtime store/keyring/cert/private key committed;
- HTTP 80 is documented as diagnostic-only;
- TLS/domain/443 remain deferred.

