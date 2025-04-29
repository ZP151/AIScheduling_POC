"""
LLM Prompts for Smart Scheduling System
This file contains all prompt templates used for LLM API calls.
"""

# Chat prompt template
CHAT_PROMPT = """
You are an intelligent scheduling assistant that can answer user questions about course scheduling, 
resource utilization, and conflict resolution. Based on user messages, provide professional, 
helpful, and friendly answers. If uncertain, you can be honest about it.

Please consider the following factors:
- Teacher availability and workload
- Classroom size and equipment
- Student schedules and workload
- Dependencies between courses
- Resource utilization efficiency

Your answers should be accurate, concise, and practical.
"""

# Constraint analysis prompt template
CONSTRAINT_ANALYSIS_PROMPT = """
Analyze the following course scheduling requirement description and extract all explicit or implicit constraints.

Requirement description:
{input}

Please return the results in JSON format, including two arrays:
1. explicitConstraints - clearly expressed requirements
2. implicitConstraints - constraints that are not explicitly expressed but can be inferred

Each constraint should include:
- id: unique identifier (explicit constraints start from 101, implicit constraints start from 201)
- name: short name of the constraint (in English)
- description: detailed description of the constraint (in English)
- type: constraint type ("Hard" for non-negotiable requirements, "Soft" for flexible preferences)
- weight: constraint weight (1.0 indicates highest priority, 0 indicates unimportance)

Notes:
- Explicit constraints can be either Hard or Soft type, depending on the wording and importance in the requirement description
- All implicit constraints must be set to "Soft" type as they are inferred by the system, not explicitly required by the user
- The weights of implicit constraints should be between 0.5 and 0.9, indicating they are flexible preferences rather than hard requirements

Please ensure you return valid JSON format without any additional text, explanations, or Markdown markup.
"""

# Conflict resolution prompt template
CONFLICT_RESOLUTION_PROMPT = """
Analyze the following course scheduling conflict and identify the root causes and potential solutions.

Conflict details:
{conflict_json}

Please return your analysis in JSON format with the following structure:
1. conflictType: A categorization of the conflict (e.g., "Resource Overlap", "Teacher Unavailability")
2. rootCauses: An array of identified root causes, each with:
   - causeDescription: Detailed description of the cause
   - severity: Severity level ("High", "Medium", "Low")
3. solutionOptions: An array of possible solutions, each with:
   - solutionDescription: Detailed description of the solution
   - impact: Description of the solution's impact on the schedule
   - feasibility: Feasibility rating (1-10, where 10 is most feasible)
   - tradeoffs: Description of any tradeoffs involved
4. recommendedSolution: The recommended solution with:
   - solutionDescription: Detailed description of the recommended solution
   - justification: Justification for why this solution is recommended
   - implementationSteps: Array of steps to implement the solution

Ensure all text is in English and in valid JSON format without any additional explanation or Markdown markup.
"""

# Schedule explanation prompt template
SCHEDULE_EXPLANATION_PROMPT = """
Explain the rationale behind the following course scheduling decision.

Schedule item:
{schedule_json}

Please return your explanation in JSON format with the following structure:
1. timeRationale: Explanation of why this time slot was chosen
2. classroomRationale: Explanation of why this classroom was selected
3. teacherRationale: Explanation of why this teacher was assigned
4. overallRationale: Overall explanation of the scheduling decision
5. alternativesConsidered: Array of alternatives that were considered but not chosen, each with:
   - type: Type of alternative ("Time", "Classroom", "Teacher")
   - alternative: Description of the alternative
   - whyNotChosen: Explanation of why this alternative was not chosen

Ensure all explanations are clear, logical, and focused on the specific scheduling decision.
Return valid JSON format without any additional text or Markdown markup.
"""

# Parameter optimization prompt template
PARAMETER_OPTIMIZATION_PROMPT = """
Analyze the following current parameters and historical data, and suggest parameter optimizations 
to improve scheduling system performance.

Current parameters:
{current_parameters}

Historical data:
{historical_data}

Please return optimization suggestions in JSON format, including two parts:
1. optimizationSuggestions - array of optimization suggestions for existing parameters
2. newParameterSuggestions - array of suggestions for new parameters to add (optional)

Each optimization suggestion should include:
- parameterName: parameter name
- currentValue: current value (as string)
- suggestedValue: suggested value (as string)
- rationale: reason for the suggestion
- expectedEffect: expected effect

Each new parameter suggestion should include:
- parameterName: parameter name
- suggestedValue: suggested value (as string)
- rationale: reason for adding this parameter
- expectedEffect: expected effect

Please ensure you return valid JSON format without any additional text, explanations, or Markdown markup.
""" 