# SmartSchedulingSystem API 后端

这个目录包含智能排课系统的后端API服务，主要提供基于LLM的智能分析和决策功能。

## 文件夹结构

- **根目录**: 核心API实现文件
  - `llm_api.py`: 主要API实现，包含5个LLM接口
  - `.env`: 环境变量文件，包含OpenAI API密钥

- **scripts/**: 用于启动和运行API的脚本
  - `run_api_8080.cmd`: **主要启动脚本**，在8080端口启动API服务

- **tests/**: API测试文件
  - `test_all_apis.py`: 综合测试所有API
  - `test_conflict_api.py`: 冲突分析API测试
  - `test_constraint_api.py`: 约束分析API测试
  - `test_explain_api.py`: 排课解释API测试
  - `test_openai.py`: OpenAI连接测试

- **templates/**: 提示模板文件
  - `llmPrompts.py`: LLM提示模板

## 主要API端点

1. `/api/llm/chat`: 自然语言对话
2. `/api/llm/analyze-constraints`: 约束分析
3. `/api/llm/analyze-conflicts`: 冲突分析
4. `/api/llm/explain-schedule`: 排课解释
5. `/api/llm/optimize-parameters`: 参数优化

## 启动指南

要启动API服务，请运行:

```
cd scripts
run_api_8080.cmd
```

服务将在 http://localhost:8080 上运行。

## 测试指南

要测试API功能，可以运行:

```
cd tests
python test_all_apis.py
```

或测试单个API:

```
python test_conflict_api.py
```

## 环境要求

请确保已安装以下依赖:
- Python 3.8+
- FastAPI
- Uvicorn
- OpenAI
- python-dotenv

确保`.env`文件包含有效的OpenAI API密钥。 