@echo off
setlocal
set "ROOT=%~dp0"
set "EXE=%ROOT%ProductionOutput\Chess2D\ChessApp.exe"
if not exist "%EXE%" call "%ROOT%package_2d.bat"
if not exist "%EXE%" exit /b 1
start "Chess 2D" "%EXE%" %*
exit /b 0
