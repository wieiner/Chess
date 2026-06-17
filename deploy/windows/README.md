# ChessOnlineServer Windows Deployment Notes

Use `ProductionOutput\ChessOnlineServer` as the portable package source. Configure `appsettings.Production.sample.json` into a local, untracked `appsettings.Production.json`, keep runtime data under `Data\`, and never commit tokens, stores, keys, certificates, or passwords.

The packaged `Deploy\windows` folder also includes:

- `Start-ChessOnlineServer-Windows.ps1`
- `Stop-ChessOnlineServer-Windows.ps1`
- `Test-ChessOnlineServer-Windows.ps1`

For the full operator flow, see `docs\CHESS3D_WINDOWS_SERVER_RUNBOOK.md` in the repository.
