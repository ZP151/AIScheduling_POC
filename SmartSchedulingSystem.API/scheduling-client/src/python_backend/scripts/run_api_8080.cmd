@echo off
echo 正在启动LLM API服务(端口8080)...

REM 检查端口8080是否被占用
netstat -ano | findstr :8080 | findstr LISTENING > nul
if %ERRORLEVEL% EQU 0 (
    echo 端口8080被占用，尝试终止进程...
    for /f "tokens=5" %%a in ('netstat -ano ^| findstr :8080 ^| findstr LISTENING') do (
        echo 尝试终止进程ID: %%a
        taskkill /PID %%a /F
    )
    timeout /t 2 > nul
)

REM 启动API服务
echo 启动API服务中...
cd ..
python llm_api.py
echo API服务已停止 