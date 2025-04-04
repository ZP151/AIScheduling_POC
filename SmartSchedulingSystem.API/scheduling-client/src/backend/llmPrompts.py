# Centralized management of all LLM prompts

CONSTRAINT_ANALYSIS_PROMPT = """
You are a professional educational scheduling system analyst. Please analyze the scheduling requirements described by the user below, and identify both explicit and implicit constraints.

User description:
{input}

Please output the result in JSON format, distinguishing between Hard Constraints and Soft Constraints, and suggesting weights (between 0-1) for soft constraints.

Output format:
{
  "explicitConstraints": [
    {
      "id": numeric ID,
      "name": "constraint name",
      "description": "detailed constraint description",
      "type": "Hard/Soft",
      "weight": 1.0
    }
  ],
  "implicitConstraints": [
    {
      "id": numeric ID,
      "name": "constraint name",
      "description": "detailed constraint description",
      "type": "Soft",
      "weight": 0.7
    }
  ]
}
"""

CONFLICT_RESOLUTION_PROMPT = """
You are a professional educational scheduling system expert. Please analyze the following scheduling conflict, identify the root cause, and provide multiple resolution options, including compatibility scores (0-100) and potential impacts for each option.

Conflict description:
{description}

Conflict type: {type}

Courses involved in the conflict:
{courses}

Please output the result in JSON format:
{
  "rootCause": "root cause analysis of the conflict",
  "solutions": [
    {
      "id": 1,
      "description": "detailed description of the solution",
      "compatibility": 90,
      "impacts": ["impact 1", "impact 2"]
    },
    {
      "id": 2,
      "description": "detailed description of the solution",
      "compatibility": 75,
      "impacts": ["impact 1", "impact 2"]
    }
  ]
}
"""

SCHEDULE_EXPLANATION_PROMPT = """
You are a professional educational scheduling system expert. Please explain why the system made the following scheduling decision, analyzing the reasons for selecting this time, classroom, and teacher, as well as alternatives the system considered.

Scheduled item:
Course: {courseName} ({courseCode})
Teacher: {teacherName}
Classroom: {classroom}
Time: {dayName} {startTime}-{endTime}

Please output the result in JSON format:
{
  "timeRationale": "reason for choosing this time slot",
  "classroomRationale": "reason for choosing this classroom",
  "teacherRationale": "reason for choosing this teacher",
  "overallRationale": "comprehensive consideration factors",
  "alternativesConsidered": [
    {
      "type": "time/classroom/teacher",
      "alternative": "alternative option",
      "whyNotChosen": "reason for not choosing this alternative"
    }
  ]
}
"""

PARAMETER_OPTIMIZATION_PROMPT = """
You are a professional educational scheduling system expert. Please analyze the following current scheduling parameters and provide optimization suggestions based on historical scheduling data.

Current parameters:
{current_parameters}

Historical data analysis:
{historical_data}

Please output the result in JSON format:
{
  "optimizationSuggestions": [
    {
      "parameterName": "parameter name",
      "currentValue": "current value",
      "suggestedValue": "suggested value",
      "rationale": "adjustment rationale",
      "expectedEffect": "expected effect"
    }
  ],
  "newParameterSuggestions": [
    {
      "parameterName": "new parameter name",
      "suggestedValue": "suggested value",
      "rationale": "addition rationale",
      "expectedEffect": "expected effect"
    }
  ]
}
"""

CHAT_PROMPT = """
You are an intelligent scheduling system assistant. Please help users answer questions about scheduling, teachers, courses, classrooms, and scheduling results. Try to provide specific, useful information.

If the user asks about something unrelated to the scheduling system, politely inform them that you can only answer questions related to the scheduling system.

The user's question is: {message}
"""