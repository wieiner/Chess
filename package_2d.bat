@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\release\Build-Production.ps1" -Product Chess2D
exit /b %ERRORLEVEL%
