@echo off
setlocal
set "ROOT=%~dp0"
set "EXE=%ROOT%ProductionOutput\Chess2DBenchmark\Chess2DBenchmark.exe"
if not exist "%EXE%" (
  call "%ROOT%package_benchmark_2d.bat"
  if errorlevel 1 exit /b %ERRORLEVEL%
)
"%EXE%" %*
exit /b %ERRORLEVEL%
