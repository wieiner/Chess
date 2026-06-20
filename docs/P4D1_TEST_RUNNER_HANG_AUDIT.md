# P4D1 Test Runner Hang Audit

Status: P4D1.4 audit.

## What happened

`tests/run-tests.ps1 -SkipBenchmark` was stopped because it ran as a large monolithic gate. The switch only skips `Chess2DBenchmark --quick`; it does not skip the solution build, contract test project builds, native tests, managed online tests, or SignalR tests.

## Old runner behavior

The previous runner did the following in one process:

1. optionally built the full `Chess.sln`;
2. built every contract test project;
3. executed every contract test executable;
4. optionally executed the benchmark.

It had no suite selection, no per-test executable timeout, no build/run split, and no durable per-test log path.

## Why this was risky

The old runner used bare `MSBuild /m`, which lets MSBuild choose uncontrolled parallelism. On this workstation that produced intermittent empty `Exit code: 1` failures during managed project builds, especially around the online SignalR test project after the server-side TFM change.

The bigger risk was runtime hanging: if `ChessOnlineSignalRContractTests.exe` waits forever on a SignalR client/server task, port issue, async wait, or teardown problem, the runner has no timeout and will wait forever too.

## Suspicious long-running area

The SignalR test is intentionally heavier than the native contract tests. It starts an in-process Kestrel app, allocates loopback ports, creates multiple SignalR clients, exercises auth, matchmaking, table actions, reconnection, concurrency, and fixture parsing. That is valuable coverage, but it must run under a timeout.

## Fix direction

P4D1.4 decomposes the runner into selectable suites and adds controlled MSBuild parallelism, per-test executable timeouts, logs under `.tmp/test-logs`, and summary output that names the exact build or executable that failed or timed out.
