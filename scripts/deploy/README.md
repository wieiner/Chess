# ChessOnlineServer Deploy Scripts

These scripts are packaging helpers and operator templates. They do not perform real SSH, cloud, Kubernetes, Redis, Azure SignalR, certificate issuance, or secret provisioning.

Linux is the intended primary deployment target. P4D1 retargets the managed server-side projects to `net8.0` and keeps WPF clients on `net8.0-windows`. The Linux package script publishes `ChessOnlineServer` for `linux-x64`; pass `-NativeLibraryPath` when a tested `libChess3DEngine.so` artifact should be included in the package.
