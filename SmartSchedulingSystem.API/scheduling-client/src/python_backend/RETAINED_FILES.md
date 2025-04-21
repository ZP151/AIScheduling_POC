# 保留的关键文件

以下是重组后保留的关键文件和目录：

## 根目录

- `llm_api.py` - 主要API实现，包含5个LLM接口
- `.env` - 环境变量文件，包含OpenAI API密钥
- `start_api.cmd` - 主启动脚本的快捷方式
- `README.md` - 项目文档

## scripts/ 目录

- `run_api_8080.cmd` - 主要启动脚本，在8080端口启动API服务

## tests/ 目录

- `test_all_apis.py` - 综合测试所有API
- `test_conflict_api.py` - 冲突分析API测试
- `test_constraint_api.py` - 约束分析API测试
- `test_explain_api.py` - 排课解释API测试
- `test_openai.py` - OpenAI连接测试
- `__init__.py` - 包初始化文件

## templates/ 目录

- `llmPrompts.py` - LLM提示模板

## 重要配置

- API服务地址: http://localhost:8080
- 主要API端点:
  - `/api/llm/chat` - 自然语言对话
  - `/api/llm/analyze-constraints` - 约束分析
  - `/api/llm/analyze-conflicts` - 冲突分析
  - `/api/llm/explain-schedule` - 排课解释
  - `/api/llm/optimize-parameters` - 参数优化 