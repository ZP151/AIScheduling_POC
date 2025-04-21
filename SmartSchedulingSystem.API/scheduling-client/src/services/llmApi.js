import axios from 'axios';

const API_URL = 'http://localhost:8080/api';

const llmApi = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Natural language chat
export const chatWithLLM = async (message, conversation = []) => {
  try {
    // 始终使用API，不使用模拟数据
    /*
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockChatResponse(message);
    }
    */
    
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
    // 始终使用API，不使用模拟数据
    /*
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockConstraintAnalysis(input);
    }
    */
    
    const response = await llmApi.post('/llm/analyze-constraints', { input });
    return response.data;
  } catch (error) {
    console.error('Constraint Analysis API Error:', error);
    throw error;
  }
};

// Conflict analysis and resolution
export const analyzeConflicts = async (conflict) => {
  console.log('llmApi服务: 开始调用冲突分析API');
  console.log('llmApi服务: 冲突数据:', conflict);
  
  // 首先定义默认的模拟响应，确保在任何情况下都能返回结果
  let mockResponse = mockConflictAnalysis(conflict);
  
  try {
    // 设置超时选项
    const options = {
      timeout: 30000, // 30秒超时
      headers: {
        'Content-Type': 'application/json'
      }
    };
    
    // 格式化冲突类型以匹配后端期望的格式
    let formattedConflict = { ...conflict };
    
    // 处理类型差异 - 将前端的类型映射到后端期望的类型
    if (formattedConflict.type) {
      if (formattedConflict.type.includes('Teachers')) {
        formattedConflict.type = 'Teacher Conflict';
      } else if (formattedConflict.type.includes('Classrooms')) {
        formattedConflict.type = 'Classroom Conflict';
      }
    }
    
    // 确保请求格式匹配后端期望的ConflictAnalysisRequest模型
    const requestData = { conflict: formattedConflict };
    
    // 调用API
    console.log('llmApi服务: 发送请求到:', `${API_URL}/llm/analyze-conflicts`);
    console.log('llmApi服务: 请求格式:', requestData);
    
    const response = await llmApi.post('/llm/analyze-conflicts', requestData, options);
    
    // 验证响应内容
    const data = response.data;
    console.log('llmApi服务: 收到原始响应:', data);
    
    if (!data) {
      console.error('llmApi服务: 响应为空');
      throw new Error('响应为空');
    }
    
    if (!data.solutions || !Array.isArray(data.solutions)) {
      console.error('llmApi服务: 响应缺少solutions数组:', data);
      throw new Error('响应格式不正确: 缺少solutions数组');
    }
    
    if (!data.rootCause) {
      console.error('llmApi服务: 响应缺少rootCause字段:', data);
      data.rootCause = '未能确定冲突根本原因';
    }
    
    // 添加标记表示这不是模拟数据
    data._isMockData = false;
    
    console.log('llmApi服务: 成功获取有效API响应:', data);
    return data;
  } catch (error) {
    console.error('llmApi服务: 冲突分析API错误:', error);
    
    // 确保模拟响应已被正确标记
    if (!mockResponse._isMockData) {
      mockResponse._isMockData = true;
    }
    
    // 记录详细的错误信息
    if (error.response) {
      console.error('llmApi服务: 错误状态码:', error.response.status);
      console.error('llmApi服务: 错误数据:', error.response.data);
      
      // 返回一个带有详细错误信息的对象，包含模拟数据
      return {
        error: true,
        httpStatus: error.response.status,
        message: `API响应错误: ${error.response.status}`,
        details: error.response.data,
        mockResponse: mockResponse
      };
    } else if (error.request) {
      console.error('llmApi服务: 未收到响应。请求对象:', error.request);
      
      return {
        error: true,
        message: '服务器未响应请求，请检查API服务是否运行',
        details: '未收到服务器响应',
        mockResponse: mockResponse
      };
    } else {
      console.error('llmApi服务: 请求配置错误:', error.message);
      
      return {
        error: true,
        message: `请求错误: ${error.message}`,
        details: error.stack,
        mockResponse: mockResponse
      };
    }
  }
};

// Schedule explanation
export const explainSchedule = async (scheduleItem) => {
  try {
    // 始终使用API，不使用模拟数据
    /*
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockScheduleExplanation(scheduleItem);
    }
    */
    
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
    // 始终使用API，不使用模拟数据
    /*
    if (process.env.NODE_ENV === 'development' && !process.env.USE_REAL_API) {
      return mockParameterOptimization(currentParameters);
    }
    */
    
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
  // 默认的通用冲突分析响应
  const defaultResponse = {
    rootCause: "Resource allocation conflicts typically occur when the same resource is required by multiple courses simultaneously. These conflicts can be resolved by adjusting scheduling times or reallocating resources.",
    solutions: [
      {
        id: 1,
        description: "Adjust the timing of one of the courses",
        compatibility: 85,
        impacts: [
          "Student schedules may need adjustment",
          "Teacher schedules may need to be modified"
        ]
      },
      {
        id: 2,
        description: "Find alternative resources (classroom or teacher)",
        compatibility: 80,
        impacts: [
          "Course quality may be slightly affected",
          "Maintains the original time schedule"
        ]
      },
      {
        id: 3,
        description: "Split the class into multiple sections",
        compatibility: 75,
        impacts: [
          "Requires additional teaching resources",
          "Can maintain original time and teacher arrangements"
        ]
      }
    ],
    _isMockData: true  // 添加标记表示这是模拟数据
  };

  // 如果没有提供冲突信息，返回默认响应
  if (!conflict) return defaultResponse;

  // 检查并标准化冲突类型
  let conflictType = conflict.type || "";
  if (typeof conflictType === 'string') {
    conflictType = conflictType.trim().toLowerCase();
  }

  // 根据冲突类型返回特定的模拟数据
  if (conflictType.includes('teacher') || conflictType.includes('教师')) {
    return {
      rootCause: "Teacher time conflicts occur when the same teacher is scheduled to teach two different courses during the same time slot. This is typically a resource allocation issue caused by the scheduling system not properly accounting for teacher availability.",
      solutions: [
        {
          id: 1,
          description: "Reschedule one of the courses to a time when the teacher is available",
          compatibility: 90,
          impacts: [
            "May require adjustments to students' other courses",
            "Teacher course load remains unchanged"
          ]
        },
        {
          id: 2,
          description: "Assign a different teacher to one of the courses",
          compatibility: 85,
          impacts: [
            "May affect course quality if the replacement teacher has less expertise",
            "Maintains the original time schedule"
          ]
        },
        {
          id: 3,
          description: "Split one course into two smaller classes taught at different times",
          compatibility: 70,
          impacts: [
            "Increases teacher workload",
            "May improve teaching quality (smaller class sizes)"
          ]
        }
      ],
      _isMockData: true  // 添加标记表示这是模拟数据
    };
  } else if (conflictType.includes('classroom') || conflictType.includes('教室')) {
    return {
      rootCause: "Classroom conflicts occur when multiple courses are scheduled in the same room at the same time. This typically happens due to limited classroom resources or the scheduling system not properly accounting for room usage.",
      solutions: [
        {
          id: 1,
          description: "Move one course to a different classroom of the same type",
          compatibility: 95,
          impacts: [
            "Need to confirm the alternative classroom has the required equipment",
            "Students may need to adjust to the new classroom location"
          ]
        },
        {
          id: 2,
          description: "Reschedule one course to a time when the classroom is available",
          compatibility: 85,
          impacts: [
            "May affect students' existing course schedules",
            "Maintains the same classroom resources"
          ]
        },
        {
          id: 3,
          description: "For smaller classes, consider combining them in a larger classroom",
          compatibility: 70,
          impacts: [
            "May require adjustments to teaching methods",
            "Improves classroom utilization efficiency"
          ]
        }
      ],
      _isMockData: true  // 添加标记表示这是模拟数据
    };
  } else {
    // 通用冲突分析
    return defaultResponse;
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