@echo off
setlocal
set "ROOT=%~dp0"
set "EXE=%ROOT%ProductionOutput\Chess3D\Chess3DApp.exe"
set "PORT=5308"

if not exist "%EXE%" (
  call "%ROOT%package_3d.bat"
  if errorlevel 1 exit /b %ERRORLEVEL%
)

if not exist "%EXE%" (
  echo Chess3D portable output was not found: "%EXE%"
  exit /b 1
)

start "Cube 3D Seat 1 Host" "%EXE%" --host --port %PORT% --seat 1
timeout /t 1 /nobreak >nul
start "Cube 3D Seat 2" "%EXE%" --connect 127.0.0.1 --port %PORT% --seat 2
start "Cube 3D Seat 3" "%EXE%" --connect 127.0.0.1 --port %PORT% --seat 3
start "Cube 3D Seat 4" "%EXE%" --connect 127.0.0.1 --port %PORT% --seat 4
start "Cube 3D Seat 5" "%EXE%" --connect 127.0.0.1 --port %PORT% --seat 5
start "Cube 3D Seat 6" "%EXE%" --connect 127.0.0.1 --port %PORT% --seat 6

echo Started six 3D chess clients on port %PORT%.
