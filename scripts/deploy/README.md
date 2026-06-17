# ChessOnlineServer Deploy Scripts

These scripts are packaging helpers and operator templates. They do not perform real SSH, cloud, Kubernetes, Redis, Azure SignalR, certificate issuance, or secret provisioning.

Linux is the intended primary deployment target, but the current server project still targets `net8.0-windows` and depends on the Windows native Chess3DEngine DLL. The Linux package script is therefore a portability audit/package scaffold until P4C removes that runtime blocker.
