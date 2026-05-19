@echo off
setlocal
set "ROOT=%~dp0"
set "EXE=%ROOT%ProductionOutput\ChessOnlineIntegrations\ChessOnlineApp.exe"
if not exist "%EXE%" call "%ROOT%package_online.bat"
if not exist "%EXE%" exit /b 1
start "Chess Online Hub" "%EXE%" %*
exit /b 0
