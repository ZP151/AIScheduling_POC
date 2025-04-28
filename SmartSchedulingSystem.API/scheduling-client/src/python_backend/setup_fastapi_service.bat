@echo off
setlocal enabledelayedexpansion

echo 📦 Step 1: 安装 NSSM（如果不存在）
set NSSM_DIR=%~dp0\nssm
set NSSM_EXE=%NSSM_DIR%\nssm.exe

if not exist "!NSSM_EXE!" (
    echo 🔄 正在下载 NSSM...
    powershell -Command "Invoke-WebRequest -Uri https://nssm.cc/release/nssm-2.24.zip -OutFile nssm.zip"
    powershell -Command "Expand-Archive -Path nssm.zip -DestinationPath . -Force"
    move /Y nssm-2.24\win64\nssm.exe "!NSSM_EXE!" > nul
    rmdir /s /q nssm-2.24
    del nssm.zip
)

echo ✅ NSSM 已准备好：!NSSM_EXE!

echo 🧠 Step 2: 检测虚拟环境路径
set PYTHON_PATH=%~dp0.venv\Scripts\python.exe
if not exist "!PYTHON_PATH!" (
    echo ❌ 错误：未找到虚拟环境的 python.exe：
    echo     !PYTHON_PATH!
    pause
    exit /b
)

echo ✅ Python 路径为: !PYTHON_PATH!

echo 🚀 Step 3: 注册 FastAPI 服务为 Windows 服务
set SERVICE_NAME=FastAPIService
set WORK_DIR=%~dp0
set SCRIPT_PATH=%~dp0llm_api.py

echo 🔧 配置服务...
"!NSSM_EXE!" install !SERVICE_NAME! "!PYTHON_PATH!" !SCRIPT_PATH!
"!NSSM_EXE!" set !SERVICE_NAME! AppParameters -m uvicorn llm_api:app --host 0.0.0.0 --port 8080
"!NSSM_EXE!" set !SERVICE_NAME! AppDirectory !WORK_DIR!
"!NSSM_EXE!" set !SERVICE_NAME! Start SERVICE_AUTO_START
"!NSSM_EXE!" set !SERVICE_NAME! DisplayName "FastAPI Service"
"!NSSM_EXE!" set !SERVICE_NAME! Description "Runs the LLM API FastAPI service at port 8080"

echo 🟢 Step 4: 启动服务...
net start !SERVICE_NAME!

echo 🎉 服务已成功注册并启动！
echo 👉 现在你可以访问： http://localhost:8080/docs
pause
