@echo off
setlocal enabledelayedexpansion

:: Step 1: Clean base directory (no trailing slash)
for %%i in ("%~dp0.") do set "BASE_DIR=%%~fi"

set NSSM_DIR=%BASE_DIR%\nssm
set NSSM_EXE=%NSSM_DIR%\nssm.exe
set PYTHON_EXE=%BASE_DIR%\.venv\Scripts\python.exe
set SERVICE_NAME=FastAPIService
set SCRIPT_PATH=%BASE_DIR%\llm_api.py

:: Step 2: Download NSSM if not present
if not exist "!NSSM_EXE!" (
    echo 🔽 Downloading NSSM...
    powershell -Command "Invoke-WebRequest -Uri https://nssm.cc/release/nssm-2.24.zip -OutFile nssm.zip"
    powershell -Command "Expand-Archive -Path nssm.zip -DestinationPath . -Force"
    mkdir "!NSSM_DIR!" >nul 2>&1
    move /Y nssm-2.24\win64\nssm.exe "!NSSM_EXE!" >nul
    rmdir /s /q nssm-2.24
    del nssm.zip
)

:: Step 3: Add NSSM to current PATH
set PATH=!NSSM_DIR!;%PATH%

:: Step 4: Add NSSM to system/user PATH (permanent)
echo 🔧 Adding NSSM to system PATH...
reg add "HKCU\Environment" /v Path /t REG_EXPAND_SZ /d "%PATH%" /f >nul

:: Step 5: Check Python
if not exist "!PYTHON_EXE!" (
    echo ❌ Python not found at: !PYTHON_EXE!
    echo Please create a virtual environment first with:
    echo python -m venv .venv
    echo .venv\Scripts\activate
    echo pip install -r requirements.txt
    pause
    exit /b
)

:: Step 6: Install FastAPI service
echo 🔧 Registering FastAPI service...
"!NSSM_EXE!" install !SERVICE_NAME! "!PYTHON_EXE!" "!SCRIPT_PATH!"
"!NSSM_EXE!" set !SERVICE_NAME! AppParameters -m uvicorn llm_api:app --host 0.0.0.0 --port 8080
"!NSSM_EXE!" set !SERVICE_NAME! AppDirectory !BASE_DIR!
"!NSSM_EXE!" set !SERVICE_NAME! Start SERVICE_AUTO_START
"!NSSM_EXE!" set !SERVICE_NAME! DisplayName "FastAPI LLM Service"
"!NSSM_EXE!" set !SERVICE_NAME! Description "LLM backend on port 8080 (FastAPI + Uvicorn)"

:: Step 7: Start service
echo ▶️ Starting FastAPIService...
net start "!SERVICE_NAME!"

:: Final output
echo ===============================================
echo ✅ FastAPI service is installed and running.
echo 🌐 Access it at: http://localhost:8080/docs
echo 🛠 You can now use 'nssm' globally in CMD.
echo ===============================================
pause
