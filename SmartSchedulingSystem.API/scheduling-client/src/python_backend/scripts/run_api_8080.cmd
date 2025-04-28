@echo off
echo Starting LLM API service (port 8080)...


REM Check if port 8080 is occupied
netstat -ano | findstr :8080 | findstr LISTENING > nul
if %ERRORLEVEL% EQU 0 (
    echo Port 8080 is occupied, trying to terminate process...
    for /f "tokens=5" %%a in ('netstat -ano ^| findstr :8080 ^| findstr LISTENING') do (
        echo Trying to terminate process ID: %%a
        taskkill /PID %%a /F
    )
    timeout /t 2 > nul
)

REM Start API service
echo Starting API service...
cd ..
python llm_api.py
echo API service stopped 