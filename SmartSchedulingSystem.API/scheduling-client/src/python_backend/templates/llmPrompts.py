"""
此文件包含用于LLM API的提示模板
"""

# 约束分析提示
CONSTRAINT_ANALYSIS_PROMPT = """
分析以下课程安排需求描述，提取所有明确或隐含的约束条件。

需求描述：
{input}

请以JSON格式返回结果，包含两个数组：
1. explicitConstraints（显式约束）- 明确表达的要求
2. implicitConstraints（隐式约束）- 未明确表达但可以推断出的约束

每个约束应包含：
- id: 唯一标识符（显式约束从101开始，隐式约束从201开始）
- name: 约束的简短名称
- description: 约束的详细描述
- type: 约束类型（"Hard"表示硬性要求，"Soft"表示灵活偏好）
- weight: 约束权重（1.0表示最高优先级，0表示无关紧要）

注意事项：
- 显式约束可以是Hard或Soft类型，取决于需求描述中的用词和重要性
- 所有隐式约束必须全部设置为"Soft"类型，因为它们是系统推断出来的，而非用户明确要求的
- 隐式约束的权重应该在0.5到0.9之间，表示它们是灵活偏好而非硬性要求

示例JSON响应格式：
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

# 冲突解决提示
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

# 调度解释提示
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

# 参数优化提示
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

# 聊天提示
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