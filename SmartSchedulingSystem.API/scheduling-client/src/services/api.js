// 导入模拟数据，用于在API失败时回退
import { mockScheduleResults, mockSemesters, mockCourses, mockTeachers, mockClassrooms } from './mockData';

// API服务 - 负责与后端API的所有通信
const API_BASE_URL = '/api'; // 使用相对路径，避免跨域问题

// 通用API请求函数
const apiRequest = async (endpoint, method = 'GET', data = null) => {
  const url = `${API_BASE_URL}${endpoint}`;
  const headers = {
    'Content-Type': 'application/json',
  };

  const config = {
    method,
    headers,
  };

  if (data) {
    config.body = JSON.stringify(data);
  }

  console.log(`Sending request to: ${url}`, { method, data });

  try {
    const response = await fetch(url, config);
    
    console.log(`Response received: ${response.status}`, response);
    
    if (!response.ok) {
      // 尝试获取API返回的错误信息
      let errorMessage;
      try {
        const errorData = await response.json();
        console.error('API error details:', errorData);
        errorMessage = errorData.message || `API request failed: ${response.status}`;
      } catch (e) {
        console.error('Unable to parse error response:', e);
        errorMessage = `API request failed: ${response.status}`;
      }
      throw new Error(errorMessage);
    }
    
    const responseData = await response.json();
    console.log('Response data:', responseData);
    return responseData;
  } catch (error) {
    console.error(`API request error (${url}):`, error);
    throw error;
  }
};

// 模拟API服务
const mockApi = {
  // 模拟生成排课方案API
  mockGenerateScheduleApi: async (formData) => {
    console.log('Using mock API to generate schedule', formData);
    
    // 模拟处理延迟
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // 使用mockData中的数据生成响应
    const semester = mockSemesters.find(s => s.id === formData.semester) || mockSemesters[0];
    const scheduleResult = mockScheduleResults[0];
    
    // 创建3个方案变体（如果请求了多个方案）
    const schedules = [];
    const solutionCount = formData.generateMultipleSolutions ? (formData.solutionCount || 3) : 1;
    
    for (let i = 1; i <= solutionCount; i++) {
      const scheduleId = 1000 + i;
      schedules.push({
        id: scheduleId,
        name: `${semester.name} Plan ${i}`,
        createdAt: new Date().toISOString(),
        status: 'Draft',
        score: 0.8 - (i - 1) * 0.1, // 让每个后续方案的评分略低
        details: scheduleResult.details.map(detail => ({
          ...detail,
          // 略微修改每个方案，使其不同
          startTime: i === 1 ? detail.startTime : 
                    i === 2 ? (parseInt(detail.startTime.split(':')[0]) + 1).toString().padStart(2, '0') + ':00' :
                    (parseInt(detail.startTime.split(':')[0]) - 1).toString().padStart(2, '0') + ':00',
          endTime: i === 1 ? detail.endTime : 
                  i === 2 ? (parseInt(detail.endTime.split(':')[0]) + 1).toString().padStart(2, '0') + ':30' :
                  (parseInt(detail.endTime.split(':')[0]) - 1).toString().padStart(2, '0') + ':30'
        }))
      });
    }
    
    return {
      schedules,
      generatedAt: new Date().toISOString(),
      totalSolutions: solutionCount,
      bestScore: schedules[0].score,
      averageScore: schedules.reduce((sum, s) => sum + s.score, 0) / schedules.length,
      primaryScheduleId: schedules[0].id
    };
  }
};

// 生成排课方案API
export const generateScheduleApi = async (formData) => {
  try {
    // 从请求中获取调试参数
    const debugOptions = formData._debug || {};
    
    // 是否启用模拟模式（设置为false以禁用模拟回退）
    const enableMockFallback = !debugOptions.disableMockFallback;
    // 是否启用调试模式（打印更多日志）
    const verboseLogging = debugOptions.verboseLogging;

    // 清理发送给后端的数据，移除调试参数
    const cleanFormData = { ...formData };
    delete cleanFormData._debug;

    // 从前端暂存的完整对象数据中获取课程、教师和教室的详细信息
    const allCourseSections = window.globalCourseData || [];
    const allTeachers = window.globalTeacherData || [];
    const allClassrooms = window.globalClassroomData || [];
    const allTimeSlots = window.globalTimeSlotData || [];
    
    // 根据ID查找完整对象
    const selectedCourseSectionObjects = (cleanFormData.courses || []).map(id => {
      // 先从全局数据查找
      let courseObj = allCourseSections.find(c => c.id === id);
      
      // 如果没找到，从mock数据查找
      if (!courseObj) {
        const mockCourse = mockCourses.find(c => c.id === id);
        if (mockCourse) {
          courseObj = {
            id: mockCourse.id,
            courseId: mockCourse.id,
            courseCode: mockCourse.code,
            courseName: mockCourse.name,
            sectionCode: `Section-${mockCourse.id}`,
            credits: 3,
            weeklyHours: 3,
            sessionsPerWeek: 2,
            hoursPerSession: 1.5,
            enrollment: mockCourse.enrollment || 40,
            departmentId: mockCourse.departmentId || 1
          };
        } else {
          // 如果在两处都找不到，使用默认值
          courseObj = {
            id: id,
            courseId: id,
            courseCode: `CS${id.toString().padStart(3, '0')}`,
            courseName: `Computer Science Course ${id}`,
            sectionCode: `Section-${id}`,
            credits: 3,
            weeklyHours: 3,
            sessionsPerWeek: 2,
            hoursPerSession: 1.5,
            enrollment: 40,
            departmentId: 1
          };
        }
      }
      return courseObj;
    });
    
    const selectedTeacherObjects = (cleanFormData.teachers || []).map(id => {
      // 先从全局数据查找
      let teacherObj = allTeachers.find(t => t.id === id);
      
      // 如果没找到，从mock数据查找
      if (!teacherObj) {
        const mockTeacher = mockTeachers.find(t => t.id === id);
        if (mockTeacher) {
          teacherObj = {
            id: mockTeacher.id,
            name: mockTeacher.name,
            code: mockTeacher.code,
            title: mockTeacher.title || "Professor",
            department: mockTeacher.department,
            departmentId: mockTeacher.departmentId,
            maxWeeklyHours: 20,
            maxDailyHours: 8,
            maxConsecutiveHours: 4
          };
        } else {
          // 如果在两处都找不到，使用默认值
          teacherObj = {
            id: id,
            name: `Prof. Faculty ${id}`,
            code: `FAC${id}`,
            title: "Professor",
            departmentId: 1,
            maxWeeklyHours: 20,
            maxDailyHours: 8,
            maxConsecutiveHours: 4
          };
        }
      }
      return teacherObj;
    });
    
    const selectedClassroomObjects = (cleanFormData.classrooms || []).map(id => {
      // 先从全局数据查找
      let classroomObj = allClassrooms.find(c => c.id === id);
      
      // 如果没找到，从mock数据查找
      if (!classroomObj) {
        const mockClassroom = mockClassrooms.find(c => c.id === id);
        if (mockClassroom) {
          classroomObj = {
            id: mockClassroom.id,
            name: mockClassroom.name,
            building: mockClassroom.building,
            capacity: mockClassroom.capacity,
            type: mockClassroom.type,
            campusId: mockClassroom.campusId,
            campusName: "Main Campus",
            hasComputers: mockClassroom.hasComputers,
            hasProjector: true
          };
        } else {
          // 如果在两处都找不到，使用默认值
          classroomObj = {
            id: id,
            name: `Room ${id.toString().padStart(3, '0')}`,
            building: "Academic Building",
            capacity: 60,
            campusId: 1,
            campusName: "Main Campus",
            type: "Regular Classroom",
            hasComputers: false,
            hasProjector: true
          };
        }
      }
      return classroomObj;
    });

    // 添加默认时间槽数据
    const timeSlotObjects = [];
    if (allTimeSlots && allTimeSlots.length > 0) {
      // 使用全局时间槽数据
      timeSlotObjects.push(...allTimeSlots);
    } else {
      // Generate default time slot data
      for (let day = 1; day <= 5; day++) {
        const dayName = ["", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"][day];
        const slots = [
          { id: (day-1)*6 + 1, startTime: "08:00", endTime: "09:30" },
          { id: (day-1)*6 + 2, startTime: "10:00", endTime: "11:30" },
          { id: (day-1)*6 + 3, startTime: "14:00", endTime: "15:30" },
          { id: (day-1)*6 + 4, startTime: "16:00", endTime: "17:30" },
          { id: (day-1)*6 + 5, startTime: "19:00", endTime: "20:30" },
          { id: (day-1)*6 + 6, startTime: "21:00", endTime: "22:30" }
        ];
        
        slots.forEach(slot => {
          timeSlotObjects.push({
            id: slot.id,
            dayOfWeek: day,
            dayName: dayName,
            startTime: slot.startTime,
            endTime: slot.endTime
          });
        });
      }
    }

    // 将前端formData转换为后端ScheduleRequestDto格式
    const scheduleRequest = {
      // 保留原始字段名称，同时映射到后端期望的字段名
      semesterId: cleanFormData.semester,
      // 同时提供ID和完整对象，增加兼容性
      courseSectionIds: cleanFormData.courses || [],
      teacherIds: cleanFormData.teachers || [],
      classroomIds: cleanFormData.classrooms || [],
      // 为了兼容性，同时保留原始字段名
      courses: cleanFormData.courses || [],
      teachers: cleanFormData.teachers || [],
      classrooms: cleanFormData.classrooms || [],
      // 添加可能的时间槽ID列表
      timeSlotIds: cleanFormData.timeSlots || [],
      timeSlots: cleanFormData.timeSlots || [], // 添加这个字段确保兼容性
      
      // 添加完整的对象数据（这是新添加的）
      courseSectionObjects: selectedCourseSectionObjects,
      teacherObjects: selectedTeacherObjects,
      classroomObjects: selectedClassroomObjects,
      timeSlotObjects: timeSlotObjects,
      
      // 添加组织范围参数
      schedulingScope: cleanFormData.schedulingScope || 'programme',
      campusId: cleanFormData.campus || null,
      schoolId: cleanFormData.school || null,
      departmentId: cleanFormData.department || null,
      programmeId: cleanFormData.programme || null,
      // 同时保留原始字段名
      campus: cleanFormData.campus || null,
      school: cleanFormData.school || null,
      department: cleanFormData.department || null,
      subject: cleanFormData.subject || null,
      programme: cleanFormData.programme || null,
      
      // 添加调度参数
      facultyWorkloadBalance: cleanFormData.facultyWorkloadBalance || 0.8,
      studentScheduleCompactness: cleanFormData.studentScheduleCompactness || 0.7,
      classroomTypeMatchingWeight: cleanFormData.classroomTypeMatchingWeight || 0.7,
      minimumTravelTime: cleanFormData.minimumTravelTime || 30,
      maximumConsecutiveClasses: cleanFormData.maximumConsecutiveClasses || 3,
      campusTravelTimeWeight: cleanFormData.campusTravelTimeWeight || 0.6,
      preferredClassroomProximity: cleanFormData.preferredClassroomProximity || 0.5,
      
      // 其他参数
      useAIAssistance: cleanFormData.useAI || false,
      constraintSettings: cleanFormData.constraintSettings || [],
      generateMultipleSolutions: cleanFormData.generateMultipleSolutions || true,
      solutionCount: cleanFormData.solutionCount || 3
    };

    if (verboseLogging) {
      console.log('Converted DTO data (detailed):', scheduleRequest);
    } else {
      console.log('Converted DTO data (basic):', {
        semester: scheduleRequest.semesterId,
        courses: scheduleRequest.courseSectionIds.length,
        teachers: scheduleRequest.teacherIds.length,
        classrooms: scheduleRequest.classroomIds.length,
        timeSlots: scheduleRequest.timeSlotObjects.length,
        constraintCount: scheduleRequest.constraintSettings.length,
        generateMultiple: scheduleRequest.generateMultipleSolutions
      });
    }

    // 尝试调用后端API
    try {
      // 使用模拟排课API而非真实排课API
      // const response = await apiRequest('/Scheduling/generate', 'POST', scheduleRequest);
      
      // // 使用测试控制器的mock-schedule端点，用随机算法排课，容易出现冲突，因此可以来测试llms的冲突分析
      // console.log('Using test controller\'s mock-schedule endpoint...');
      // const response = await apiRequest('/Test/mock-schedule', 'POST', scheduleRequest);
       
      // 使用排课控制器的generate端点
       console.log('Using Schedule controller\'s generate endpoint...');
       // 使用level1级别的约束
       // const response = await apiRequest('/Schedule/generate', 'POST', scheduleRequest);
       // 使用level2级别的约束，加上了两种可用性约束
       const response = await apiRequest('/Schedule/generate-advanced', 'POST', scheduleRequest);
       
      console.log('Schedule generation successful:', response);
      return response;
    } catch (apiError) {
      console.error('Schedule API call failed:', apiError);
      
      // 如果启用了模拟回退，在真实API失败时使用模拟数据
      if (enableMockFallback) {
        console.warn('Fallback to mock data...');
        return await mockApi.mockGenerateScheduleApi(cleanFormData);
      } else {
        // 否则，将错误向上抛出
        throw apiError;
      }
    }
  } catch (error) {
    console.error('Error occurred while generating schedule:', error);
    throw error;
  }
};

// 获取排课历史记录
export const getScheduleHistory = async (semesterId, limit = 10) => {
  try {
    // 在查询中添加limit参数，默认获取最近10条记录
    const results = await apiRequest(`/Scheduling/history/${semesterId}?limit=${limit}`, 'GET');
    
    // 将后端返回的结果转换为前端期望的格式
    // 现在，历史记录将包含排课请求ID和关联的排课方案列表
    return results.map(result => ({
      requestId: result.requestId,
      generatedAt: result.generatedAt,
      semesterName: result.semesterName || `Semester ${semesterId}`,
      totalSolutions: result.totalSolutions || result.schedules?.length || 1,
      bestScore: result.bestScore || 0.0,
      // 对于每个排课请求，包含多个排课方案
      schedules: (result.schedules || []).map(schedule => ({
        id: schedule.scheduleId || schedule.id,
        name: schedule.name || `Schedule ${schedule.scheduleId || schedule.id}`,
        createdAt: schedule.createdAt,
        status: schedule.status,
        score: schedule.score || 0.0,
        statusHistory: schedule.statusHistory || [],
        isPrimary: schedule.isPrimary || false,
        details: (schedule.items || schedule.details || []).map(item => ({
          courseCode: item.courseCode,
          courseName: item.courseName,
          teacherName: item.teacherName,
          classroom: `${item.building || ''}${item.building ? '-' : ''}${item.classroomName || item.classroom || ''}`,
          day: item.dayOfWeek !== undefined ? item.dayOfWeek : item.day,
          dayName: item.dayName,
          startTime: item.startTime,
          endTime: item.endTime
        }))
      }))
    }));
  } catch (error) {
    console.error('Failed to get schedule history:', error);
    // 如果API失败，返回模拟数据作为后备方案
    return generateMockScheduleHistory(semesterId, limit);
  }
};

// 生成模拟的排课历史记录
const generateMockScheduleHistory = (semesterId, limit = 10) => {
  const mockHistory = [];
  
  // 生成最近的10个排课记录
  for (let i = 0; i < limit; i++) {
    const requestId = 1000 - i;
    const date = new Date();
    date.setDate(date.getDate() - i * 2); // 每两天一个记录
    
    // 每个请求生成1-3个排课方案
    const solutionCount = Math.floor(Math.random() * 3) + 1;
    const schedules = [];
    
    for (let j = 0; j < solutionCount; j++) {
      // 生成随机状态，较新的记录更可能是Draft状态
      let status = 'Draft';
      if (i > 2) {
        const statuses = ['Draft', 'Published', 'Canceled', 'Archived'];
        const statusIndex = Math.floor(Math.random() * statuses.length);
        status = statuses[statusIndex];
      }
      
      // 生成状态历史
      const statusHistory = [];
      statusHistory.push({
        status: 'Draft',
        timestamp: new Date(date.getTime() - 3600000).toISOString(),
        userId: 'System'
      });
      
      if (status !== 'Draft') {
        statusHistory.push({
          status: status,
          timestamp: date.toISOString(),
          userId: 'Admin'
        });
      }
      
      schedules.push({
        id: requestId * 10 + j,
        name: `Schedule ${semesterId}-${requestId}-${j + 1}`,
        createdAt: date.toISOString(),
        status: status,
        score: 0.9 - (j * 0.1),
        statusHistory: statusHistory,
        isPrimary: j === 0,
        details: generateMockScheduleDetails(5 + j)
      });
    }
    
    mockHistory.push({
      requestId: requestId,
      generatedAt: date.toISOString(),
      semesterName: `Semester ${semesterId}`,
      totalSolutions: solutionCount,
      bestScore: 0.9,
      schedules: schedules
    });
  }
  
  return mockHistory;
};

// 生成模拟的排课详情
const generateMockScheduleDetails = (courseCount) => {
  const details = [];
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  const timeSlots = [
    { start: '08:00', end: '09:30' },
    { start: '10:00', end: '11:30' },
    { start: '14:00', end: '15:30' },
    { start: '16:00', end: '17:30' }
  ];
  const teachers = ['Prof. Smith', 'Prof. Johnson', 'Prof. Williams', 'Prof. Brown', 'Prof. Davis'];
  const buildings = ['A', 'B', 'C'];
  
  for (let i = 0; i < courseCount; i++) {
    const dayIndex = Math.floor(Math.random() * 5) + 1; // 星期一到星期五
    const timeSlotIndex = Math.floor(Math.random() * timeSlots.length);
    const teacherIndex = Math.floor(Math.random() * teachers.length);
    const buildingIndex = Math.floor(Math.random() * buildings.length);
    const roomNumber = Math.floor(Math.random() * 400) + 100;
    
    details.push({
      courseCode: `CS${(101 + i).toString().padStart(3, '0')}`,
      courseName: `Computer Science Course ${i + 1}`,
      teacherName: teachers[teacherIndex],
      classroom: `${buildings[buildingIndex]}-${roomNumber}`,
      day: dayIndex,
      dayName: days[dayIndex],
      startTime: timeSlots[timeSlotIndex].start,
      endTime: timeSlots[timeSlotIndex].end
    });
  }
  
  return details;
};

// 根据ID获取排课方案
export const getScheduleById = async (scheduleId) => {
  try {
    const result = await apiRequest(`/Scheduling/${scheduleId}`, 'GET');
    
    // 将后端返回的结果转换为前端期望的格式
    return {
      id: result.scheduleId,
      name: `Schedule ${result.scheduleId}`,
      createdAt: result.createdAt,
      status: result.status,
      details: result.items.map(item => ({
        courseCode: item.courseCode,
        courseName: item.courseName,
        teacherName: item.teacherName,
        classroom: `${item.building}-${item.classroomName}`,
        day: item.dayOfWeek,
        dayName: item.dayName,
        startTime: item.startTime,
        endTime: item.endTime
      }))
    };
  } catch (error) {
    console.error('Failed to get schedule:', error);
    throw error;
  }
};

// 发布排课方案
export const publishSchedule = async (scheduleId) => {
  try {
    return await apiRequest(`/Scheduling/publish/${scheduleId}`, 'PUT');
  } catch (error) {
    console.error('Failed to publish schedule:', error);
    throw error;
  }
};

// 取消排课方案
export const cancelSchedule = async (scheduleId) => {
  try {
    return await apiRequest(`/Scheduling/cancel/${scheduleId}`, 'PUT');
  } catch (error) {
    console.error('Failed to cancel schedule:', error);
    throw error;
  }
};

// 获取时间槽数据
export const getTimeSlotsApi = async () => {
  try {
    const timeSlots = await apiRequest('/TimeSlot', 'GET');
    return timeSlots;
  } catch (error) {
    console.error('Failed to get time slot data:', error);
    throw error;
  }
};
