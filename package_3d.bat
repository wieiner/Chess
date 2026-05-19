@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\release\Build-Production.ps1" -Product Chess3D
exit /b %ERRORLEVEL%
