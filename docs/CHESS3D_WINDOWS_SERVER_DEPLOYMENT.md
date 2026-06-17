# Windows Server Deployment

Windows remains the executable deployment target in P4B.

Use `ProductionOutput/ChessOnlineServer` as the portable folder. Copy `appsettings.Production.sample.json` to a local config file, create a runtime `Data` folder, and run behind your preferred Windows service wrapper.

The templates in `deploy/windows` are intentionally conservative notes, not an automatic privileged installer.
