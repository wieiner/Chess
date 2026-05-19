@echo off
setlocal
set "ROOT=%~dp0"
echo User-facing executables:
echo.
echo 2D chess:       "%ROOT%ProductionOutput\Chess2D\ChessApp.exe"
echo 3D chess:       "%ROOT%ProductionOutput\Chess3D\Chess3DApp.exe"
echo Rubik studio:   "%ROOT%ProductionOutput\Rubik\RubikApp.exe"
echo Online hub:     "%ROOT%ProductionOutput\ChessOnlineIntegrations\ChessOnlineApp.exe"
echo 2D benchmark:   "%ROOT%ProductionOutput\Chess2DBenchmark\Chess2DBenchmark.exe"
echo.
echo Launch scripts:
echo   run_chess_2d.bat
echo   run_chess_3d.bat
echo   run_rubik.bat
echo   run_online.bat
echo   run_benchmark_2d.bat
echo   run_3d_six_clients.bat
echo.
echo Packaging scripts:
echo   package_all.bat
echo   package_2d.bat
echo   package_3d.bat
echo   package_rubik.bat
echo   package_online.bat
echo   package_benchmark_2d.bat
echo   clean_outputs.bat
exit /b 0
