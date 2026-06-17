# ChessOnlineServer Windows Deployment Notes

Use `ProductionOutput\ChessOnlineServer` as the portable package source. Configure `appsettings.Production.sample.json` into a local, untracked `appsettings.Production.json`, keep runtime data under `Data\`, and never commit tokens, stores, keys, certificates, or passwords.
