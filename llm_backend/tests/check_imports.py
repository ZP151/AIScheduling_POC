"""
Simple test file to check that all required libraries can be imported correctly
"""

try:
    import fastapi
    import uvicorn
    import pydantic 
    import openai
    from dotenv import load_dotenv
    
    print("All libraries imported successfully!")
    print(f"FastAPI version: {fastapi.__version__}")
    print(f"Uvicorn version: {uvicorn.__version__}")
    print(f"Pydantic version: {pydantic.__version__}")
    print(f"OpenAI version: {openai.__version__}")
    
except ImportError as e:
    print(f"Import error: {e}") 