# SmartSchedulingSystem API Backend

This directory contains the backend API service for the Smart Scheduling System, which primarily provides intelligent analysis and decision-making functionality based on LLM.

## Directory Structure

- **Root**: Core API implementation files
  - `llm_api.py`: Main API implementation, containing 5 LLM interfaces
  - `.env`: Environment variables file containing OpenAI API key

- **templates/**: Prompt template files
  - `llm_prompts.py`: LLM prompt templates

- **tests/**: API test files
  - `test_all_apis.py`: Comprehensive test for all APIs
  - `test_conflict_api.py`: Conflict analysis API test
  - `test_constraint_api.py`: Constraint analysis API test
  - `test_explain_api.py`: Schedule explanation API test
  - `test_openai.py`: OpenAI connection test

## Main API Endpoints

1. `/api/llm/chat`: Natural language conversation
2. `/api/llm/analyze-constraints`: Constraint analysis
3. `/api/llm/analyze-conflicts`: Conflict analysis
4. `/api/llm/explain-schedule`: Schedule explanation
5. `/api/llm/optimize-parameters`: Parameter optimization

## Environment Setup

### Python Version Requirement
This project requires **Python 3.10.6** or higher. To check your Python version:

```bash
python --version
```

If you don't have Python 3.10.6+ installed, download it from [python.org](https://www.python.org/downloads/).

### Virtual Environment Setup

It's strongly recommended to use a virtual environment:

```bash
# Navigate to the backend directory
cd SmartSchedulingSystem.API/scheduling-client/src/python_backend

# Create a virtual environment
python -m venv .venv

# Activate the virtual environment
# On Windows:
.venv\Scripts\activate
# On Linux/Mac:
# source .venv/bin/activate

# Install dependencies
pip install -r requirements.txt
```

## Starting the Service

There are multiple ways to start the API service:

### Method 1: Using Python Scripts

```bash
# Activate virtual environment first
.venv\Scripts\activate

# Run the service startup script
python run_api.py
```

This script automatically handles port conflicts and starts the uvicorn server.

### Method 2: Using Batch Scripts

```bash
# From root directory
run_api_8080.cmd
```

### Method 3: Direct Uvicorn Command

```bash
# Activate virtual environment first
.venv\Scripts\activate

# Start with uvicorn directly
uvicorn llm_api:app --host 0.0.0.0 --port 8080 --reload
```

Benefits of this approach:
- Hot reload: Server automatically restarts when code changes
- Detailed logging
- Better error messages for debugging

### Method 4: Direct Module Execution

```bash
# Activate virtual environment first
.venv\Scripts\activate

# Direct execution of the module
python llm_api.py
```

## Installing as Windows Service

For production deployment, you can install the API as a Windows service:

```bash
# Run with Administrator privileges
setup_fastapi_service.bat
```

This will:
1. Download NSSM (Non-Sucking Service Manager) if not present
2. Register the FastAPI application as a Windows service
3. Configure it to start automatically with Windows
4. Start the service

To uninstall the service:

```bash
# Run with Administrator privileges
uninstall-python-service.bat
```

## Testing Guide

To test API functionality, run:

```bash
# Activate virtual environment first
.venv\Scripts\activate

cd tests
python test_all_apis.py
```

Or test specific API:

```bash
python test_conflict_api.py
```

## Environment Requirements

Please ensure the following dependencies are installed:
- Python 3.10.6+
- FastAPI
- Uvicorn
- OpenAI
- python-dotenv

Make sure the `.env` file contains a valid OpenAI API key.

## Development Guidelines

- Follow PEP 8 style guidelines
- Add appropriate error handling for API endpoints
- Document any changes to the LLM prompts
- Run tests before submitting changes
- Keep template files organized in the templates directory 