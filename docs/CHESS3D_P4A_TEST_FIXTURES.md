# Chess3D P4A Test Fixtures

P4A adds descriptor fixtures under:

- `assets/rules/scenarios/chess3d/identity`
- `assets/rules/scenarios/chess3d/persistence`

These are not new game modes. They are regression descriptors for hosted online infrastructure.

Identity fixtures cover:

- register/login token issuance;
- spoofed player id rejection.

Persistence fixtures cover:

- room/table/seat/action-log persistence;
- no runtime secrets or key material in portable packages.

`ChessOnlineSignalRContractTests` executes the runtime behavior and parses these descriptors. `scripts/verify.ps1` checks that representative descriptors are copied to development output and `ProductionOutput`.
