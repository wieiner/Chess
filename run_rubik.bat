@echo off
setlocal
set "ROOT=%~dp0"
set "EXE=%ROOT%ProductionOutput\Rubik\RubikApp.exe"
if not exist "%EXE%" call "%ROOT%package_rubik.bat"
if not exist "%EXE%" exit /b 1
start "Rubik Studio" "%EXE%" %*
exit /b 0
