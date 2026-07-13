# P4K SignalR Logging Audit

Date: 2026-07-13

## Result

Status: **PASS with one hardening gap**

The application does not intentionally log access tokens, refresh tokens,
passwords, authorization headers, SignalR connection IDs, savegame JSON, or
runtime persistence content. Detailed SignalR errors are disabled. Client and
operator reports use redaction/short IDs and raw files are ignored.

The remaining gap is framework request-start logging at `Information` level.
ASP.NET Core may include a SignalR `access_token` query parameter in request
URLs for transports that cannot use an Authorization header. The current
remote journal contains no such query token, but configuration should prevent
future occurrence rather than relying on current client behavior.

No code or server configuration changes are made in Phase 37.

## Detailed Errors

`HostedOnlineOptions.EnableDetailedErrors` defaults to false.

Tracked development/local settings explicitly set it to false. The production
sample omits the property and therefore retains the false code default.
`ChessOnlineServerHost` passes only this normalized value to
`AddSignalR(...).EnableDetailedErrors`.

Hub registry exceptions are logged server-side and converted to the generic
client response:

- reason: `internalError`;
- text: `Internal server error.`

Persistence-presence warnings log only connected state and exception type.
They do not include player, room, table, token, savegame, or filesystem data.

Risk: hub methods that throw outside the registry wrapper still rely on
SignalR's generic exception behavior. With detailed errors disabled, exception
details are not sent to clients. Future methods should continue returning safe
protocol errors explicitly.

## Token Handling

The authentication handler reads bearer material from either:

- the `Authorization` header; or
- SignalR's conventional `access_token` query parameter.

It does not log either value. `OnlineTokenService` protects/unprotects values
and stores only the refresh-token hash in the durable auth session record.

Client/smoke code keeps issued credentials in memory and prints short player
labels only. `ChessOnlineSecretRedactor` removes bearer and token-like values
from textual client reports.

## Framework Request Logging Gap

`ChessOnlineServerHost` clears providers and adds console/debug providers, but
does not configure category filters. The deployed service currently emits
ASP.NET Core request-start/end Information logs.

Microsoft's SignalR security guidance notes that these URL logs can contain an
`access_token` query parameter. Recommended low-risk remediation:

1. set `Microsoft.AspNetCore.Hosting` to `Warning` or above for hosted public
   deployments;
2. set `Microsoft.AspNetCore.Http.Connections` to `Warning` unless short-lived
   operator diagnostics explicitly require more;
3. never add generic query-string or header logging;
4. if custom request logging is later required, log route/path only and remove
   `access_token` before formatting;
5. retain application aggregate logs at Information where they contain no IDs
   or secrets.

This filter should be code/config tested before the next server deployment. It
does not make HTTP 80 safe for real credentials; TLS remains required.

## Connection Identifiers

Full connection IDs exist only in internal transport registries and are needed
for SignalR group cleanup. They are not serialized in public lobby/spectator
DTOs and are not emitted by current server log templates.

Client reconnect diagnostics expose a short display form rather than the full
ID. Spectator replacement connection ID is marked `JsonIgnore` and remains
server-internal.

No connection token is available to or logged by application code.

## Reports And Ignored Output

The repository ignores `.tmp/`, which contains:

- watchdog stdout/stderr;
- test logs;
- remote operator smoke logs;
- manual WPF smoke reports;
- deploy package work directories.

Tracked result documents contain only sanitized aggregate outcomes, build
identity, package hashes, state hashes, and safe status text. Raw logs and
runtime IDs are not committed.

## Remote Read-only Evidence

A marker-only scan of the last 24 hours of `chessonline.service` journal was
performed without printing matching content:

- journal lines inspected: 5019;
- `access_token=` query lines: 0;
- password marker lines: 0;
- literal `ConnectionId` lines: 0;
- request-start Information lines: 659;
- relay URL lines: 616.

Three lines matched the word `Authorization`; no bearer value or header content
was printed or retained. These are consistent with framework authorization
category/status messages, but raw production logs remain sensitive and are not
copied into the repository.

## HTTP 80 Boundary

Even perfect log redaction does not protect credentials in transit over plain
HTTP. The public endpoint remains diagnostic/dev only:

- temporary generated users only;
- no real passwords;
- no ranked/production accounts;
- no claim of confidentiality;
- TLS/domain/443 deferred and untouched.

## Follow-up

Phase 38 should define abuse/rate policy. A later low-risk implementation can
add explicit framework logging filters together with rate/readiness hardening,
tests, package identity, backup, and guarded deployment.
