"""
This file contains prompt templates for the LLM API
"""

# 约束分析提示
CONSTRAINT_ANALYSIS_PROMPT = """
Analyze the following course scheduling requirements description, extract all explicit or implicit constraints.

Requirements description:
{input}

Please return the result in JSON format, containing two arrays:
1. explicitConstraints (explicit constraints) - explicitly stated requirements
2. implicitConstraints（implicit constraints）- constraints that are not explicitly stated but can be inferred

Each constraint should include:
- id: unique identifier (explicit constraints start from 101, implicit constraints start from 201)
- name: short name of the constraint
- description: detailed description of the constraint
- type: constraint type ("Hard" for hard requirements, "Soft" for flexible preferences)
- weight: constraint weight (1.0 for highest priority, 0 for无关紧要)

Notes:
- explicit constraints can be either "Hard" or "Soft" type, depending on the wording and importance in the requirements description
- all implicit constraints must be set to "Soft" type, as they are system inferred rather than user explicitly stated
- implicit constraints should have a weight between 0.5 and 0.9, indicating they are flexible preferences rather than hard requirements

Example JSON response format:
{
  "explicitConstraints": [
    {
      "id": 101,
      "name": "Course Duration Constraint",
      "description": "Each class session is 2 hours long",
      "type": "Hard",
      "weight": 1.0
    }
  ],
  "implicitConstraints": [
    {
      "id": 201,
      "name": "Course Spread Constraint",
      "description": "Advanced Mathematics and Physics courses should not be on the same day due to heavy workload for students",
      "type": "Soft",
      "weight": 0.7
    }
  ]
}
"""

# Conflict resolution prompt
CONFLICT_RESOLUTION_PROMPT = """
Analyze the following course scheduling conflict, identify the root cause, and propose solutions.

Conflict description:
Type: {type}
Description: {description}
Involved courses:
{courses}

Please provide the response in English as a JSON object with the following fields:
1. rootCause - Analysis of the root cause of the conflict
2. solutions - Array of possible solutions

Each solution should include:
- id: Unique identifier
- description: Description of the solution
- compatibility: Compatibility score with the overall schedule (0-100)
- impacts: Array of impacts from implementing this solution

Example JSON response format:
{
  "rootCause": "The conflict is a resource allocation issue of teacher time conflict type. Specifically, Professor Zhang has been scheduled for two different courses at the same time on Monday from 9:00-11:00",
  "solutions": [
    {
      "id": 1,
      "description": "Adjust the course time to avoid the conflict period",
      "compatibility": 90,
      "impacts": [
        "Student schedules may need to be rearranged",
        "Teaching quality will not be affected"
      ]
    }
  ]
}
"""

# Schedule explanation prompt
SCHEDULE_EXPLANATION_PROMPT = """
Explain the reasoning behind the following course scheduling decision, including why specific time, classroom, and teacher were chosen.

Course scheduling details:
- Course name: {courseName} ({courseCode})
- Teacher: {teacherName}
- Classroom: {classroom}
- Day: {dayName}
- Time: {startTime}-{endTime}

Please provide the explanation in English as a JSON object with the following fields:
1. timeRationale - Why this time slot was chosen
2. classroomRationale - Why this classroom was chosen
3. teacherRationale - Why this teacher was assigned
4. overallRationale - Overall explanation of the integrated decision
5. alternativesConsidered - Array of alternatives that were considered, each containing:
   - type: Type of alternative (time/classroom/teacher)
   - alternative: The alternative option
   - whyNotChosen: Reason why this alternative was not selected

Example JSON response format:
{
  "timeRationale": "Advanced Mathematics was scheduled on Monday at 9:00-11:00 because this time has shown high student engagement and coordinates well with other related courses.",
  "classroomRationale": "Science Building Room 301 was selected because it has sufficient capacity for all students and is equipped with the specialized equipment needed for this course. The location is also convenient for students coming from other classes.",
  "teacherRationale": "Professor Zhang was chosen because their expertise closely matches the course content, and they have no other teaching commitments during this time slot. The teacher has also expressed preference for this time slot.",
  "overallRationale": "Scheduling Advanced Mathematics on Monday at 9:00-11:00 represents an optimal solution considering teacher preferences, classroom availability, student needs, and course requirements. This arrangement maximizes teaching quality and resource utilization.",
  "alternativesConsidered": [
    {
      "type": "time",
      "alternative": "Tuesday 9:00-11:00",
      "whyNotChosen": "This time slot conflicted with other required courses for many students"
    },
    {
      "type": "classroom",
      "alternative": "Science Building Room 304",
      "whyNotChosen": "While the size was appropriate, it lacked the specialized equipment needed for this course"
    },
    {
      "type": "teacher",
      "alternative": "Professor Wang",
      "whyNotChosen": "Professor Wang has relevant expertise but already has other teaching commitments during this time slot"
    }
  ]
}
"""

# Parameter optimization prompt
PARAMETER_OPTIMIZATION_PROMPT = """
Analyze the current scheduling parameters and historical data to provide optimization suggestions.

Current parameters:
{current_parameters}

Historical data:
{historical_data}

Please provide the response in English as a JSON object with the following fields:
1. optimizationSuggestions - Array of suggestions for optimizing current parameters
2. newParameterSuggestions - Array of suggestions for new parameters to add

Each optimization suggestion should include:
- parameterName: Name of the parameter
- currentValue: Current value
- suggestedValue: Suggested value
- rationale: Reason for the suggestion
- expectedEffect: Expected effect of the change

Each new parameter suggestion should include:
- parameterName: Name of the parameter
- suggestedValue: Suggested value
- rationale: Reason for adding this parameter
- expectedEffect: Expected effect of adding this parameter

Example JSON response format:
{
  "optimizationSuggestions": [
    {
      "parameterName": "Teacher Workload Balance Weight",
      "currentValue": "0.7",
      "suggestedValue": "0.8",
      "rationale": "Increasing the teacher workload balance weight will better distribute teaching tasks and prevent faculty overload.",
      "expectedEffect": "More balanced teacher workload distribution, improving faculty satisfaction and teaching quality."
    }
  ],
  "newParameterSuggestions": [
    {
      "parameterName": "Course Continuity Weight",
      "suggestedValue": "0.7",
      "rationale": "Adding a course continuity parameter can optimize the arrangement sequence of related courses.",
      "expectedEffect": "Related courses will be arranged in a logical order and spacing, improving learning continuity."
    }
  ]
}
"""

# Chat prompt
CHAT_PROMPT = """
You are an intelligent scheduling assistant capable of answering questions about course scheduling, resource utilization, and conflict resolution.
Based on the user's message, provide professional, helpful, and friendly responses. Be honest if you're unsure about something.

User message: {message}

Please consider the following factors:
- Teacher availability and workload
- Classroom size and equipment
- Student schedules and workload
- Course dependencies
- Resource utilization efficiency

Your answers should be accurate, concise, and practical.
"""