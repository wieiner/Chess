@echo off
setlocal
set "ROOT=%~dp0"
set "EXE=%ROOT%ProductionOutput\Chess3D\Chess3DApp.exe"
if not exist "%EXE%" call "%ROOT%package_3d.bat"
if not exist "%EXE%" exit /b 1
start "Chess 3D" "%EXE%" %*
exit /b 0
