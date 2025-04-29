@echo off
setlocal

set SERVICE_NAME=FastAPIService
set NSSM_EXE=%~dp0nssm\nssm.exe

:: Check if NSSM exists
if not exist "%NSSM_EXE%" (
    echo Error: Cannot find NSSM at %NSSM_EXE%
    pause
    exit /b
)

:: Check if service exists
sc query "%SERVICE_NAME%" >nul 2>&1
if errorlevel 1 (
    echo Warning: Service "%SERVICE_NAME%" does not exist.
) else (
    echo [INFO] Stopping service...
    net stop "%SERVICE_NAME%"

    echo [INFO] Uninstalling service...
    "%NSSM_EXE%" remove "%SERVICE_NAME%" confirm
)

echo Done.
pause 