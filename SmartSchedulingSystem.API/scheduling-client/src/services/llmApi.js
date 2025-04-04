import axios from 'axios';

const API_URL = 'http://localhost:8000/api';

const llmApi = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Natural language chat
export const chatWithLLM = async (message, conversation = []) => {
  try {
    // For quick development/testing, we can mock the response
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockChatResponse(message);
    }
    
    const response = await llmApi.post('/llm/chat', { message, conversation });
    return response.data;
  } catch (error) {
    console.error('Chat API Error:', error);
    return { response: "I'm sorry, I encountered an error. Please try again later." };
  }
};

// Constraint identification and analysis
export const analyzeConstraints = async (input) => {
  try {
    // For quick development/testing, we can mock the response
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockConstraintAnalysis(input);
    }
    
    const response = await llmApi.post('/llm/analyze-constraints', { input });
    return response.data;
  } catch (error) {
    console.error('Constraint Analysis API Error:', error);
    throw error;
  }
};

// Conflict analysis and resolution
export const analyzeConflicts = async (conflict) => {
  try {
    // For quick development/testing, we can mock the response
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockConflictAnalysis(conflict);
    }
    
    const response = await llmApi.post('/llm/analyze-conflicts', { conflict });
    return response.data;
  } catch (error) {
    console.error('Conflict Analysis API Error:', error);
    throw error;
  }
};

// Schedule explanation
export const explainSchedule = async (scheduleItem) => {
  try {
    // For quick development/testing, we can mock the response
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockScheduleExplanation(scheduleItem);
    }
    
    const response = await llmApi.post('/llm/explain-schedule', { scheduleItem });
    return response.data;
  } catch (error) {
    console.error('Schedule Explanation API Error:', error);
    throw error;
  }
};

// Parameter optimization
export const optimizeParameters = async (currentParameters, historicalData = null) => {
  try {
    // For quick development/testing, we can mock the response
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockParameterOptimization(currentParameters);
    }
    
    const response = await llmApi.post('/llm/optimize-parameters', { 
      currentParameters, 
      historicalData 
    });
    return response.data;
  } catch (error) {
    console.error('Parameter Optimization API Error:', error);
    throw error;
  }
};

// Mock responses for development (when backend is not available)
const mockChatResponse = (message) => {
  // Basic response based on keywords in the message
  if (message.toLowerCase().includes('conflict')) {
    return { 
      response: "I can see there are currently 2 scheduling conflicts. One involves Professor Smith having two courses at the same time, and another is a classroom conflict in Building B. You can use the conflict resolution tool to get detailed solutions." 
    };
  } else if (message.toLowerCase().includes('optimization')) {
    return { 
      response: "Based on the current parameter settings, I recommend reducing the Teacher Time Preference weight slightly and increasing the Resource Utilization constraint weight. This would help balance faculty workload while improving overall classroom utilization." 
    };
  } else if (message.toLowerCase().includes('utilization')) {
    return { 
      response: "The current classroom utilization rate is approximately 78%. Building A has higher utilization (85%) compared to Building B (71%). There are several time slots on Friday afternoons that have particularly low utilization rates." 
    };
  } else {
    return { 
      response: "I'm here to help with your scheduling needs. You can ask me about conflicts, parameter optimization, utilization rates, or any other scheduling-related questions." 
    };
  }
};

const mockConstraintAnalysis = (input) => {
  // Simple mock that extracts basic constraints from the input
  return {
    explicitConstraints: [
      { 
        id: 101, 
        name: 'Course Duration Constraint', 
        description: 'Each class session is 2 hours long', 
        type: 'Hard', 
        weight: 1.0 
      },
      { 
        id: 102, 
        name: 'Teacher Availability Constraint', 
        description: 'Professor Zhang is only available on Monday and Wednesday mornings', 
        type: 'Hard', 
        weight: 1.0 
      },
      { 
        id: 103, 
        name: 'Classroom Capacity Constraint', 
        description: 'Need a classroom that can accommodate 120 students', 
        type: 'Hard', 
        weight: 1.0 
      },
      { 
        id: 104, 
        name: 'Equipment Requirement Constraint', 
        description: 'Need a classroom with projection equipment', 
        type: 'Hard', 
        weight: 1.0 
      }
    ],
    implicitConstraints: [
      { 
        id: 201, 
        name: 'Course Spread Constraint', 
        description: 'Advanced Mathematics and Physics courses should not be on the same day due to heavy workload for students', 
        type: 'Soft', 
        weight: 0.7 
      },
      { 
        id: 202, 
        name: 'Large Course Classroom Preference', 
        description: 'Prefer appropriate large classrooms for large courses', 
        type: 'Soft', 
        weight: 0.9 
      }
    ]
  };
};

const mockConflictAnalysis = (conflict) => {
  // Mock conflict analysis based on conflict type
  if (conflict.type === 'Teacher Conflict') {
    return {
      rootCause: "Professor Smith is the preferred teacher for both Introduction to Computer Science and Algorithm Design, and both courses are scheduled at the same time on Monday mornings due to high demand for this time slot.",
      solutions: [
        {
          id: 1,
          description: "Reassign Algorithm Design to Professor Johnson, who also has expertise in this subject area.",
          compatibility: 85,
          impacts: [
            "Professor Johnson's teaching load will increase by 2 hours per week",
            "Overall teaching quality remains high as Professor Johnson is also qualified for this course"
          ]
        },
        {
          id: 2,
          description: "Reschedule Algorithm Design to Wednesday 08:00-09:30, keeping the same teacher.",
          compatibility: 90,
          impacts: [
            "Classroom would need to be changed to A-201",
            "No student conflicts detected"
          ]
        },
        {
          id: 3,
          description: "Keep the same teacher but reschedule Introduction to Computer Science to Monday 10:00-11:30.",
          compatibility: 75,
          impacts: [
            "Time conflicts with other courses for 3 students",
            "Would require swapping with Data Structures course"
          ]
        }
      ]
    };
  } else if (conflict.type === 'Classroom Conflict') {
    return {
      rootCause: "Building B-301 is one of the few large classrooms equipped with specialized equipment needed for both Physics I and Advanced Mathematics, and Tuesday 10:00-11:30 is a prime time slot when most students are available.",
      solutions: [
        {
          id: 1,
          description: "Move Physics I to classroom B-302, which has similar equipment and capacity.",
          compatibility: 95,
          impacts: [
            "No changes to time schedule required",
            "B-302 has slightly older equipment but meets all requirements"
          ]
        },
        {
          id: 2,
          description: "Reschedule Advanced Mathematics to Tuesday 08:00-09:30, keeping the same classroom.",
          compatibility: 80,
          impacts: [
            "5 students have conflicts with other courses at this time",
            "Professor Williams has indicated slight preference against early morning classes"
          ]
        },
        {
          id: 3,
          description: "Reschedule Physics I to Thursday 10:00-11:30 in the same classroom.",
          compatibility: 70,
          impacts: [
            "8 students have conflicts with other courses at this time",
            "Professor Brown would need to rearrange office hours"
          ]
        }
      ]
    };
  } else {
    return {
      rootCause: "This conflict appears to be due to competing requirements for limited resources during high-demand time slots.",
      solutions: [
        {
          id: 1,
          description: "Reschedule one of the conflicting courses to a different time slot.",
          compatibility: 85,
          impacts: [
            "May cause some student schedule disruptions",
            "All core requirements still met"
          ]
        },
        {
          id: 2,
          description: "Find an alternative room or teacher depending on the specific conflict.",
          compatibility: 80,
          impacts: [
            "Might require adjusting other course parameters",
            "Maintains original time preferences"
          ]
        }
      ]
    };
  }
};

const mockScheduleExplanation = (scheduleItem) => {
  // Generate explanation based on the course code
  const courseCode = scheduleItem.courseCode;
  
  if (courseCode === 'CS101') {
    return {
      timeRationale: "Monday morning at 8:00 AM was selected because it's the preferred time for Professor Smith and historically has the highest student engagement rates for introductory courses.",
      classroomRationale: "Building A-101 was chosen because it has computers required for practical demonstrations and its capacity of 120 seats matches the expected enrollment of 105 students.",
      teacherRationale: "Professor Smith was assigned as he is the course coordinator for Introduction to Computer Science and has the highest student satisfaction ratings for this course.",
      overallRationale: "This scheduling decision optimally balances teacher preference, classroom resource utilization, and student learning outcomes. The Monday morning slot for CS101 also fits well with the overall Computer Science curriculum flow through the week.",
      alternativesConsidered: [
        {
          type: "Time",
          alternative: "Wednesday 08:00-09:30",
          whyNotChosen: "Would create a scheduling conflict with another core CS course, potentially splitting the student cohort."
        },
        {
          type: "Classroom",
          alternative: "Building B-401",
          whyNotChosen: "While it has newer computers, it's located far from the CS department and has excessive capacity (180 seats) for this class size."
        },
        {
          type: "Teacher",
          alternative: "Professor Johnson",
          whyNotChosen: "Already has a full teaching load this semester and specializes more in advanced programming topics."
        }
      ]
    };
  } else if (courseCode === 'MATH101') {
    return {
      timeRationale: "Tuesday morning was selected for Advanced Mathematics based on historical data showing better performance for quantitative courses in morning slots, and to avoid conflict with other major courses.",
      classroomRationale: "Building B-301 was chosen due to its large capacity (150 students) matching the expected enrollment of 142, and its tiered seating arrangement which is optimal for mathematics instruction.",
      teacherRationale: "Professor Williams is the primary instructor for Advanced Mathematics and has specialized expertise in this subject area.",
      overallRationale: "This scheduling decision prioritizes student learning outcomes by placing a challenging quantitative course in a morning slot while ensuring adequate classroom space for the large enrollment. The location in Building B is also convenient for science and engineering students who make up 65% of the course enrollment.",
      alternativesConsidered: [
        {
          type: "Time",
          alternative: "Monday 10:00-11:30",
          whyNotChosen: "Would create a back-to-back schedule with calculus courses for many students, potentially impacting comprehension."
        },
        {
          type: "Classroom",
          alternative: "Building A-201",
          whyNotChosen: "Insufficient capacity (80 seats) for the expected enrollment of 142 students."
        },
        {
          type: "Teacher",
          alternative: "Adjunct Professor Davis",
          whyNotChosen: "Less familiar with the department's specific curriculum integration points with other courses."
        }
      ]
    };
  } else {
    // Generic explanation for other courses
    return {
      timeRationale: `This time slot was selected based on teacher availability, classroom availability, and to minimize conflicts with other courses in the same study program.`,
      classroomRationale: `The classroom was chosen to match the expected enrollment size and to provide any specialized equipment needed for this particular course.`,
      teacherRationale: `The teacher was assigned based on expertise in the subject matter, scheduling availability, and teaching load balance.`,
      overallRationale: `This scheduling decision represents an optimal balance of various constraints including student needs, faculty preferences, and resource utilization.`,
      alternativesConsidered: [
        {
          type: "Time",
          alternative: "A different time slot",
          whyNotChosen: "Would have caused conflicts with other core courses or reduced overall schedule quality."
        },
        {
          type: "Classroom",
          alternative: "A different classroom",
          whyNotChosen: "Either lacked capacity, required facilities, or had lower overall suitability scores."
        },
        {
          type: "Teacher",
          alternative: "A different teacher",
          whyNotChosen: "Had less expertise in this specific subject area or would have created workload imbalance."
        }
      ]
    };
  }
};

const mockParameterOptimization = (currentParameters) => {
  // Generate optimization suggestions based on current parameters
  return {
    optimizationSuggestions: [
      {
        parameterName: "Faculty Workload Balance",
        currentValue: currentParameters.facultyWorkloadBalance?.toString() || "0.8",
        suggestedValue: "0.7",
        rationale: "Current weight is causing uneven distribution of classes among faculty. Some teachers are overloaded while others have light schedules.",
        expectedEffect: "More balanced workload across faculty, increasing overall satisfaction and teaching quality."
      },
      {
        parameterName: "Student Schedule Compactness",
        currentValue: currentParameters.studentScheduleCompactness?.toString() || "0.7",
        suggestedValue: "0.8",
        rationale: "Historical data shows that students perform better with more compact schedules that minimize gaps between classes.",
        expectedEffect: "Improved student attendance and reduced campus congestion between class periods."
      },
      {
        parameterName: "Classroom Type Matching Weight",
        currentValue: currentParameters.classroomTypeMatchingWeight?.toString() || "0.7",
        suggestedValue: "0.9",
        rationale: "Analysis of past semesters shows significant correlation between appropriate classroom type and student performance.",
        expectedEffect: "Better learning outcomes for specialized courses, potentially improving overall course satisfaction by 12%."
      }
    ],
    newParameterSuggestions: [
      {
        parameterName: "Related Courses Clustering",
        suggestedValue: "0.6",
        rationale: "Analysis shows that scheduling courses from the same major together in close proximity improves student attendance and performance.",
        expectedEffect: "Each major's courses will be more optimally distributed, reducing schedule fragmentation by approximately 30%."
      },
      {
        parameterName: "Class Break Distribution",
        suggestedValue: "0.5",
        rationale: "Current schedules don't account for optimal break distribution throughout the day, causing student fatigue.",
        expectedEffect: "Improved student attentiveness in afternoon classes, potentially increasing performance by 8-10%."
      }
    ]
  };
};

export default {
  chatWithLLM,
  analyzeConstraints,
  analyzeConflicts,
  explainSchedule,
  optimizeParameters
};