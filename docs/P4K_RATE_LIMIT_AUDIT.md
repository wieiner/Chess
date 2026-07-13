# P4K Rate Limit And Abuse Audit

Date: 2026-07-13

## Result

Status: **policy defined; implementation gated by tests**

The server has a basic per-connection hub command guard, but it is not a full
abuse boundary. HTTP auth endpoints are not rate-limited, some hub methods do
not pass through the guard, and reconnecting creates a fresh connection
partition. Phase 38 defines a conservative policy without changing code or the
deployed server.

## Existing Guard

`OnlineHubConnectionRegistry.AllowCommand` stores timestamps per SignalR
`ConnectionId` and rejects after `RateLimitPermitLimit` within
`RateLimitWindowSeconds`.

Strengths:

- simple immediate rejection;
- no request queue;
- applies before registry mutation for methods using `InvokeRegistry`;
- timestamps are removed when the connection is removed.

Limitations:

- partition identity is transient connection ID, not authenticated player;
- a reconnect resets the effective budget;
- one global cost treats ping-like reads and native preview/action work equally;
- timestamp lists allocate and scan on each call;
- no injected clock for deterministic boundary tests;
- matchmaking methods use their own validation path;
- `Hello`, `Ping`, hub diagnostics, and lobby/matchmaking listing paths are not
  uniformly covered;
- HTTP auth/health/diagnostics endpoints are outside this guard;
- the rejection uses generic `illegalAction` rather than a dedicated safe
  throttling code/retry hint.

## Cost Classification

| Operation | Surface | Cost | Mutation | Primary abuse risk |
| --- | --- | --- | --- | --- |
| live health | HTTP | cheap | no | probe flood |
| ready health | HTTP | medium | no | filesystem/profile checks |
| diagnostics | HTTP/hub | medium | no | enumeration/probe flood |
| register | HTTP | expensive | yes | account/store growth, password hashing |
| login | HTTP | expensive | yes | credential guessing, hashing, lockout pressure |
| refresh | HTTP | medium | yes | token validation/session reads |
| logout | HTTP | medium | yes | session mutation |
| Hello/Ping | hub | cheap | session | connection churn |
| matchmaking status/list | hub | cheap | no | polling flood |
| matchmaking join/cancel | hub | medium | yes | queue churn, duplicate pairing |
| lobby refresh | hub | medium | no | table enumeration |
| JoinSpectator | hub | medium | yes | registry/group churn |
| RequestResumeMatch | hub | medium | yes | seat/group/persistence activity |
| RequestLegalPreview | hub | expensive | no | native legal-action generation |
| RequestSnapshot | hub | expensive | no | native savegame JSON export |
| RequestActionLog | hub | medium | no | log copy/serialization |
| SubmitAction | hub | expensive | yes | native mutation, hash, persistence, broadcast |

## Partition Keys

Priority order:

1. authenticated stable `PlayerId` for hub and authenticated HTTP operations;
2. normalized remote IP for register/login and unauthenticated requests;
3. a fixed `unknown` partition only when no safe IP is available.

Do not use:

- access or refresh token;
- Authorization header;
- password or username/password combination;
- SignalR connection token;
- raw query string;
- full connection ID in logs/diagnostics.

For forwarded IP, trust only the already configured loopback nginx proxy. Never
accept arbitrary client-supplied forwarded headers as the partition identity.

## Conservative Candidate Policies

These are initial test candidates, not deployed promises.

### HTTP

| Policy | Permit/window | Queue | Partition |
| --- | --- | --- | --- |
| register | 5 / 10 minutes | 0 | remote IP |
| login | 10 / minute | 0 | remote IP |
| refresh | 30 / minute | 0 | remote IP, later validated session ID |
| logout | 30 / minute | 0 | authenticated player or IP |
| `/api/auth/me` | 60 / minute | 0 | authenticated player |
| diagnostics | 30 / minute | 0 | remote IP |
| ready | 60 / minute | 0 | remote IP |
| live | 300 / minute | 0 | remote IP |

Health limits must remain high enough for systemd/operator monitoring. Loopback
health can be exempt or use a separate generous partition, but public health
must never share the auth budget.

HTTP rejection is status 429 with a small fixed JSON body. Do not echo partition
keys, credentials, query strings, endpoint internals, or stack traces. A bounded
`Retry-After` may be returned when the limiter supplies one.

### Hub method classes

| Class | Methods | Permit/window | Queue |
| --- | --- | --- | --- |
| cheap-read | Ping, matchmaking status/list | 120 / minute | 0 |
| state-read | lobby, action log | 60 / minute | 0 |
| native-read | legal preview, snapshot | 40 / minute | 0 |
| membership | matchmaking join/cancel, spectator join, resume | 20 / minute | 0 |
| table-control | ready, start | 20 / minute | 0 |
| mutation | submit action | 30 / minute | 0 |

Each authenticated player receives independent budgets. Method-class budgets
prevent cheap polling from consuming action permits and prevent preview floods
from blocking resume. Existing per-connection global guard may remain at a high
defense-in-depth ceiling during migration.

Hub rejection should use an append-only reason such as `rateLimited` with fixed
safe text and optional retry seconds. It must not be represented as a legal move
failure and must not mutate board, sequence, action log, lobby, queue, seats, or
state hash.

## Account Abuse Boundary

The account service already has password rules and login-attempt lockout. Rate
limits complement, not replace, those controls:

- register budget constrains durable account growth;
- login budget constrains per-IP bursts while account lockout constrains a
  targeted username;
- error bodies should avoid adding account-enumeration detail;
- successful normal temp-user creation must fit comfortably within the policy;
- remote smoke creates several temporary users and therefore needs a distinct
  operator run ID, not a bypass token.

The diagnostic HTTP 80 environment still permits only generated temporary
credentials. Rate limiting does not provide transport confidentiality.

## State And Memory Bounds

A method limiter implementation must:

- use `TimeProvider` for deterministic tests;
- remove idle player/IP partitions after a bounded retention;
- cap partition count or use framework-managed limiter lifecycle;
- avoid unbounded timestamp lists;
- expose aggregate accepted/rejected counters only;
- avoid logging partition keys;
- honor cancellation;
- use queue limit 0 for all listed policies.

## Test And Load Gate

Before implementation is deployed:

1. first permitted request succeeds;
2. exact boundary succeeds/fails deterministically with fake time;
3. request after window renewal succeeds;
4. two players/IPs have isolated budgets;
5. reconnect does not reset authenticated player budget;
6. no raw token/header/password is a partition or log value;
7. 429 body is fixed and secret-free;
8. hub rejection leaves hash/seq/log unchanged;
9. normal one-app, two-window, resume, spectator, lobby, and `all` smoke stay
   below limits;
10. parallel action submission remains authority-serialized;
11. health monitoring remains usable;
12. a small load simulation proves bounded memory/partition cleanup.

## Phase 39 Gate

Low-risk implementation is allowed only with deterministic tests. Recommended
first scope:

- ASP.NET Core fixed-window HTTP policies for auth and public diagnostics;
- a small injected player/method-class hub guard;
- dedicated `rateLimited` protocol rejection;
- framework request-log category filtering from Phase 37.

Do not deploy in the same implementation commit. Phase 40 readiness and a full
green gate must precede the Phase 41 package/deploy workflow.
