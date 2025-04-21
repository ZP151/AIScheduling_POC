"""
简单的测试文件，检查所有需要的库是否可以正确导入
"""

try:
    import fastapi
    import uvicorn
    import pydantic 
    import openai
    from dotenv import load_dotenv
    
    print("所有库导入成功！")
    print(f"FastAPI版本: {fastapi.__version__}")
    print(f"Uvicorn版本: {uvicorn.__version__}")
    print(f"Pydantic版本: {pydantic.__version__}")
    print(f"OpenAI版本: {openai.__version__}")
    
except ImportError as e:
    print(f"导入错误: {e}") 