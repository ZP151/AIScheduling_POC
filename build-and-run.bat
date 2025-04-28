@echo off
setlocal enabledelayedexpansion

:: === CONFIGURATION ===
set PORT=5001
set PYTHON_PORT=8080
set DEPLOY_DIR=C:\Deploy\SmartSchedulingSystem
set CLIENT_DIR=%~dp0SmartSchedulingSystem.API\scheduling-client
set API_DIR=%~dp0SmartSchedulingSystem.API
set PYTHON_SCRIPTS_DIR=%CLIENT_DIR%\src\python_backend\scripts

echo [INFO] Start deployment process...

:: === 1. Kill old .NET API process on PORT %PORT% ===
echo [1] Checking and killing old API service on port %PORT%...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :%PORT%') do (
    echo [INFO] Killing PID %%a
    taskkill /F /PID %%a
)

:: === 2. Kill old Python Backend process on PORT %PYTHON_PORT% ===
echo [2] Checking and killing old Python service on port %PYTHON_PORT%...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :%PYTHON_PORT%') do (
    echo [INFO] Killing PID %%a
    taskkill /F /PID %%a
)

:: === 3. Build React frontend ===
echo [3] Building React frontend...
cd /d %CLIENT_DIR%
call npm install
call npm run build

if exist "%CLIENT_DIR%\build" (
    echo [✔] React build generated successfully.
) else (
    echo [ERROR] React build failed! Exiting...
    pause
    exit /b
)

:: === 4. Copy React build to wwwroot ===
echo [4] Copying React build to API wwwroot...
cd /d %API_DIR%
if exist "wwwroot" (
    echo [INFO] Removing old wwwroot...
    rmdir /s /q wwwroot
)
xcopy "%CLIENT_DIR%\build" "wwwroot" /E /Y /I

if exist "wwwroot" (
    echo [✔] React build copied successfully to wwwroot.
) else (
    echo [ERROR] Copy to wwwroot failed! Exiting...
    pause
    exit /b
)

:: === 5. Publish .NET API ===
echo [5] Publishing .NET API...
cd /d %API_DIR%
call dotnet publish SmartSchedulingSystem.API.csproj -c Release -o %DEPLOY_DIR%

if exist "%DEPLOY_DIR%\SmartSchedulingSystem.API.dll" (
    echo [✔] API published successfully.
) else (
    echo [ERROR] API publish failed! Exiting...
    pause
    exit /b
)

:: === 6. Start .NET API ===
echo [6] Starting .NET API on port %PORT%...
cd /d %DEPLOY_DIR%
start "API" cmd /k dotnet SmartSchedulingSystem.API.dll --urls "http://0.0.0.0:%PORT%"

:: === 7. Start Python backend ===
echo [7] Starting Python backend...
cd /d %PYTHON_SCRIPTS_DIR%

if exist "C:\Users\Dell\source\repos\SmartSchedulingSystem\SmartSchedulingSystem.API\scheduling-client\src\python_backend\.venv\Scripts\activate.bat" (
    call C:\Users\Dell\source\repos\SmartSchedulingSystem\SmartSchedulingSystem.API\scheduling-client\src\python_backend\.venv\Scripts\activate.bat
) else (
    echo [ERROR] Cannot find .venv\Scripts\activate.bat! Please check virtual environment.
    pause
    exit /b
)

if exist "run_api_8080.cmd" (
    start "PythonBackend" cmd /k run_api_8080.cmd
) else (
    echo [ERROR] Cannot find run_api_8080.cmd! Please check backend scripts.
    pause
    exit /b
)

:: === Deployment Complete ===
echo.
echo [✔] All services started successfully!
echo Visit http://localhost:%PORT% or http://<your-ip>:%PORT%
pause
endlocal
