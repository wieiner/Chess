# P4K Post-smoke Server Inventory

Date: 2026-07-13

## Scope

This is a read-only post-smoke inventory of the shared Hetzner host. No service
was restarted or reloaded, no file or container was changed, and no nginx, UFW,
TLS, port 443, x-ui/Xray, Outline, Albatronix, Unreal, PostgreSQL, DNS, or runtime
store configuration was read or modified.

## ChessOnline

- `chessonline.service`: active/running, restart count 0, main exit status 0.
- Kestrel: `127.0.0.1:5077`, owned by the ChessOnline `dotnet` process.
- nginx: active and listening on public TCP 80.
- loopback live: `Healthy`.
- loopback ready: ready, protocol `chess3d.relay.v1`, profile count 5.
- recent 30-minute journal counts: zero for unhandled exception, persistence
  error, duplicate sequence, native authority failure, and permission denied.

Only numeric risk counts were retained. Raw journal records, unit contents,
environment, credentials, keyrings, and runtime stores were not collected.

## Neighbor Services

| Service/boundary | Read-only observation | Result |
| --- | --- | --- |
| x-ui/Xray | `x-ui.service` active; Xray process owns TCP 443 | Unchanged and not touched |
| Outline | `outline-ss-serv` owns TCP/UDP 22527; `shadowbox` container up | Running and not touched |
| Albatronix | `albatronix-sse-server` up on TCP 3000 | Running and not touched |
| Albatronix PostgreSQL | container up and healthy, internal 5432 | Running and not touched |
| Watchtower | container up and healthy | Running and not touched |
| Unreal SYServer | no UDP 7777 listener, matching process, or running candidate service observed | Not claimed as running; no action taken |

The absence of UDP 7777 was verified separately with the UDP socket table and a
process/service-name check. The P4K predeploy inventory also did not record a
7777 listener, so the available evidence does not attribute this absence to the
current smoke work. Bringing that unrelated service up is outside P4K scope.

## Capacity

- memory: 3.7 GiB total, 2.8 GiB available;
- root disk: 75 GiB total, 61 GiB available, 16% used;
- ChessOnline remained healthy after all remote regression runs.

## Firewall Snapshot

UFW was read only and remained active. Existing allow entries included TCP 80,
TCP 443, TCP 10443, UDP 7777, and OpenSSH (plus IPv6 equivalents). No firewall
mutation command was issued. An allow rule does not imply that an application is
currently listening, which explains why UDP 7777 remains listed in UFW while no
Unreal listener is observable.

## Conclusion

ChessOnline, nginx, x-ui/Xray, Outline, and the observed Docker workloads remain
healthy after the operator smoke. No neighboring service was changed. The only
non-confirmation is Unreal SYServer: it is not currently observable and therefore
must not be reported as active without a separate owner-approved investigation.
