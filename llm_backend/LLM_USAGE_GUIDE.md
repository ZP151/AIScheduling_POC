# LLM Feature Usage Guide

## Overview

The Smart Scheduling System integrates 5 LLM functionalities:

1. **Intelligent Assistant**
2. **Requirement Analyzer**
3. **Conflict Resolution**
4. **Parameter Optimization**
5. **Schedule Explanation**

## Prerequisites

To ensure all LLM functionalities work properly, make sure:

1. A valid OpenAI API key is set in the `.env` file
2. The backend Python FastAPI service is running
3. The frontend application is started

### Starting the Backend API Service

**Method 1: Using the Startup Script**

```bash
# Navigate to the backend directory
cd SmartSchedulingSystem.API/scheduling-client/src/python_backend

# Start the API service
python run_api.py
```

**Method 2: Using Direct Uvicorn Command**

```bash
# Navigate to the backend directory
cd SmartSchedulingSystem.API/scheduling-client/src/python_backend

# Start the API service with uvicorn
uvicorn llm_api:app --host 0.0.0.0 --port 8080 --reload
```

The service will run at `http://localhost:8080` and provide all necessary API endpoints.

**Command Parameters:**
- `--host 0.0.0.0`: Allows access from any IP address (use carefully in production)
- `--port 8080`: Specifies the port for the service
- `--reload`: Enables hot reloading for development

### Starting the Frontend Application

```bash
# Navigate to the frontend directory
cd SmartSchedulingSystem.API/scheduling-client

# Install dependencies (if not already installed)
npm install

# Start the application
npm start
```

The frontend application will run at `http://localhost:3000`.

## Feature Testing Guide

### 1. Intelligent Assistant

- Click the intelligent assistant icon in the bottom right corner to open the chat window
- Enter scheduling-related questions, such as:
  - "Why are Computer Science courses scheduled in Building B?"
  - "How can I optimize the current scheduling plan?"
  - "How many course conflicts are there this semester?"
  - "Analyze the current classroom utilization rate"

### 2. Requirement Analyzer

- On the course scheduling form page, find the "Scheduling Requirement Analyzer" component
- Enter scheduling requirements in natural language, for example:
  - "We need to schedule an Advanced Mathematics course, twice a week, 2 hours each time. Professor Zhang is only available on Monday and Wednesday mornings. The class has about 120 students and needs a classroom with projection equipment. Also, this course should preferably not be scheduled on the same day as Physics because it would be too demanding for students."
- Click the "Analyze Constraints" button
- The system will automatically extract explicit and implicit constraints
- You can click "Add All Constraints to System" to add the analysis results to the scheduling system

### 3. Conflict Resolution

- On the scheduling results page, the system displays detected conflicts
- For each conflict, click the "Intelligent Analysis" button
- The system will analyze the root cause of the conflict and provide multiple solutions
- Review the compatibility score and potential impact of each solution
- Select the most suitable solution and click "Apply This Solution"

### 4. Parameter Optimization

- On the system settings or advanced options page, find the "Intelligent Parameter Optimization" button
- Click the button, and the system will analyze current parameter settings and historical data (if available)
- Review suggested parameter adjustments and new parameter suggestions
- You can select specific suggestions to apply
- Click "Apply Selected Suggestions" to update system parameters

### 5. Schedule Explanation

- On the scheduling results page, there is a question mark icon next to each scheduling item
- Click the icon to see why the system made specific scheduling decisions
- The system will explain the reasons for choosing specific times, classrooms, and teachers
- It will also display alternatives the system considered but did not choose, and why they were rejected

## Troubleshooting

1. **API Connection Errors**
   - Ensure the Python backend service is running
   - Check if there are CORS errors in the console; you may need to add allowed origins in the backend

2. **Parsing Errors**
   - Check the Python backend console logs to see the raw JSON format returned by the API
   - The API automatically uses mock data as a fallback, so even if LLM response parsing fails, the system will still display reasonable results

3. **OpenAI API Limitations**
   - If you encounter rate limit errors, try reducing the request frequency or upgrading your API plan

4. **Model Scale Limitations**
   - The LLM API uses the gpt-3.5-turbo model; for more complex analysis, consider upgrading to a more powerful model in `llm_api.py` 
