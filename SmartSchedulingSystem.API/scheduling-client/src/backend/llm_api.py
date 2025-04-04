from fastapi import FastAPI, HTTPException, Body
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Dict, Any, Optional
import openai
import os
import json
import re
from dotenv import load_dotenv

from llmPrompts import (
    CONSTRAINT_ANALYSIS_PROMPT, 
    CONFLICT_RESOLUTION_PROMPT,
    SCHEDULE_EXPLANATION_PROMPT,
    PARAMETER_OPTIMIZATION_PROMPT,
    CHAT_PROMPT
)

# Load environment variables
load_dotenv()

# Configure OpenAI API
openai.api_key = os.getenv("OPENAI_API_KEY")

app = FastAPI()

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify exact domains
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Models
class ChatMessage(BaseModel):
    role: str
    content: str

class ChatRequest(BaseModel):
    message: str
    conversation: Optional[List[ChatMessage]] = []

class ConstraintAnalysisRequest(BaseModel):
    input: str

class ConflictAnalysisRequest(BaseModel):
    conflict: Dict[str, Any]

class ScheduleExplanationRequest(BaseModel):
    scheduleItem: Dict[str, Any]

class ParameterOptimizationRequest(BaseModel):
    currentParameters: Dict[str, Any]
    historicalData: Optional[Dict[str, Any]] = None

# Helper function to parse JSON from AI responses
def parse_json_response(response_text):
    try:
        return json.loads(response_text)
    except json.JSONDecodeError:
        # If not standard JSON, try to extract JSON part
        json_match = re.search(r'({[\s\S]*})', response_text)
        if json_match:
            return json.loads(json_match.group(1))
        else:
            raise HTTPException(status_code=422, detail="AI returned result is not valid JSON format")

# API routes
@app.post("/api/llm/chat")
async def chat_endpoint(request: ChatRequest):
    # Use imported CHAT_PROMPT
    prompt = CHAT_PROMPT.format(message=request.message)
    
    messages = [{"role": "system", "content": prompt}]
    
    # Add conversation history
    for msg in request.conversation:
        messages.append({"role": msg.role, "content": msg.content})
    
    # Add current user message
    messages.append({"role": "user", "content": request.message})
    
    try:
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=messages,
            temperature=0.7,
            max_tokens=1000,
        )
        return {"response": response.choices[0].message.content.strip()}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/llm/analyze-constraints")
async def analyze_constraints(request: ConstraintAnalysisRequest):
    try:
        # Use imported CONSTRAINT_ANALYSIS_PROMPT
        prompt = CONSTRAINT_ANALYSIS_PROMPT.format(input=request.input)
        
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[
                {"role": "system", "content": "You are a scheduling system analysis expert"},
                {"role": "user", "content": prompt}
            ],
            temperature=0.3,
            max_tokens=1000,
        )
        
        response_text = response.choices[0].message.content.strip()
        return parse_json_response(response_text)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/llm/analyze-conflicts")
async def analyze_conflicts(request: ConflictAnalysisRequest):
    try:
        conflict = request.conflict
        courses_str = "\n".join([f"- Course: {c.get('name')} ({c.get('code')}), Teacher: {c.get('teacher')}, Classroom: {c.get('classroom')}, Time: {c.get('timeSlot')}" 
                                for c in conflict.get("involvedCourses", [])])
        
        # Use imported CONFLICT_RESOLUTION_PROMPT
        prompt = CONFLICT_RESOLUTION_PROMPT.format(
            description=conflict.get("description", ""),
            type=conflict.get("type", "Unknown"),
            courses=courses_str
        )
        
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[
                {"role": "system", "content": "You are a scheduling conflict resolution expert"},
                {"role": "user", "content": prompt}
            ],
            temperature=0.3,
            max_tokens=1000,
        )
        
        response_text = response.choices[0].message.content.strip()
        return parse_json_response(response_text)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/llm/explain-schedule")
async def explain_schedule(request: ScheduleExplanationRequest):
    try:
        item = request.scheduleItem
        
        # Use imported SCHEDULE_EXPLANATION_PROMPT
        prompt = SCHEDULE_EXPLANATION_PROMPT.format(
            courseName=item.get("courseName", ""),
            courseCode=item.get("courseCode", ""),
            teacherName=item.get("teacherName", ""),
            classroom=item.get("classroom", ""),
            dayName=item.get("dayName", ""),
            startTime=item.get("startTime", ""),
            endTime=item.get("endTime", "")
        )
        
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[
                {"role": "system", "content": "You are a scheduling decision explanation expert"},
                {"role": "user", "content": prompt}
            ],
            temperature=0.3,
            max_tokens=1000,
        )
        
        response_text = response.choices[0].message.content.strip()
        return parse_json_response(response_text)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/llm/optimize-parameters")
async def optimize_parameters(request: ParameterOptimizationRequest):
    try:
        current_parameters = json.dumps(request.currentParameters, ensure_ascii=False, indent=2)
        historical_data = json.dumps(request.historicalData, ensure_ascii=False, indent=2) if request.historicalData else "No historical data available"
        
        # Use imported PARAMETER_OPTIMIZATION_PROMPT
        prompt = PARAMETER_OPTIMIZATION_PROMPT.format(
            current_parameters=current_parameters,
            historical_data=historical_data
        )
        
        response = openai.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[
                {"role": "system", "content": "You are a scheduling parameter optimization expert"},
                {"role": "user", "content": prompt}
            ],
            temperature=0.3,
            max_tokens=1000,
        )
        
        response_text = response.choices[0].message.content.strip()
        return parse_json_response(response_text)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# Run server
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)