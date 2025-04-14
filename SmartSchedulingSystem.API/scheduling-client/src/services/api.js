// 导入模拟数据，用于在API失败时回退
import { mockScheduleResults, mockSemesters } from './mockData';

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

  console.log(`发送请求到: ${url}`, { method, data });

  try {
    const response = await fetch(url, config);
    
    console.log(`收到响应: ${response.status}`, response);
    
    if (!response.ok) {
      // 尝试获取API返回的错误信息
      let errorMessage;
      try {
        const errorData = await response.json();
        console.error('API错误详情:', errorData);
        errorMessage = errorData.message || `API请求失败: ${response.status}`;
      } catch (e) {
        console.error('无法解析错误响应:', e);
        errorMessage = `API请求失败: ${response.status}`;
      }
      throw new Error(errorMessage);
    }
    
    const responseData = await response.json();
    console.log('响应数据:', responseData);
    return responseData;
  } catch (error) {
    console.error(`API请求错误 (${url}):`, error);
    throw error;
  }
};

// 模拟API服务
const mockApi = {
  // 模拟生成排课方案API
  mockGenerateScheduleApi: async (formData) => {
    console.log('使用模拟API生成排课方案', formData);
    
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
        name: `${semester.name} 方案 ${i}`,
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

    // 将前端formData转换为后端ScheduleRequestDto格式
    const scheduleRequest = {
      // 保留原始字段名称，同时映射到后端期望的字段名
      semesterId: cleanFormData.semester,
      // 同时提供两种字段名称，增加兼容性
      courseSectionIds: cleanFormData.courses || [],
      teacherIds: cleanFormData.teachers || [],
      classroomIds: cleanFormData.classrooms || [],
      // 为了兼容性，同时保留原始字段名
      courses: cleanFormData.courses || [],
      teachers: cleanFormData.teachers || [],
      classrooms: cleanFormData.classrooms || [],
      // 添加可能的时间槽ID列表
      timeSlotIds: cleanFormData.timeSlots || [],
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
      console.log('转换后的DTO数据(详细):', scheduleRequest);
    } else {
      console.log('转换后的DTO数据(基本):', {
        semesterId: scheduleRequest.semesterId,
        coursesCount: scheduleRequest.courseSectionIds.length,
        teachersCount: scheduleRequest.teacherIds.length,
        classroomsCount: scheduleRequest.classroomIds.length
      });
    }

    try {
      // 尝试调用真实API
      console.log('尝试调用真实API...');
      const result = await apiRequest('/Scheduling/generate', 'POST', scheduleRequest);
      
      // 将后端返回的结果转换为前端期望的格式
      // 处理排课方案列表
      const schedules = result.solutions.map(solution => ({
        id: solution.scheduleId,
        name: `Schedule ${solution.scheduleId}`,
        createdAt: solution.createdAt,
        status: solution.status,
        score: solution.score,
        details: solution.items.map(item => ({
          courseCode: item.courseCode,
          courseName: item.courseName,
          teacherName: item.teacherName,
          classroom: `${item.building}-${item.classroomName}`,
          day: item.dayOfWeek,
          dayName: item.dayName,
          startTime: item.startTime,
          endTime: item.endTime
        }))
      }));

      // 按评分排序，分数高的在前
      schedules.sort((a, b) => b.score - a.score);

      // 检查是否存在错误信息
      if (result.errorMessage) {
        console.warn('排课算法返回错误信息:', result.errorMessage);
        // 如果有错误信息但仍然返回了排课方案，我们仍然可以使用方案
        if (schedules.length === 0) {
          // 如果没有方案，抛出错误
          throw new Error(result.errorMessage);
        }
      }

      return {
        schedules,
        generatedAt: result.generatedAt,
        totalSolutions: result.totalSolutions,
        bestScore: result.bestScore,
        averageScore: result.averageScore,
        errorMessage: result.errorMessage // 传递错误信息
      };
    } catch (error) {
      // 如果API调用失败并且启用了模拟回退
      if (enableMockFallback) {
        console.warn('真实API调用失败，回退到使用模拟API:', error);
        if (verboseLogging) {
          console.log('API请求详情:', {
            url: '/Scheduling/generate',
            method: 'POST',
            data: scheduleRequest,
            error: {
              message: error.message,
              stack: error.stack
            }
          });
        }
        return await mockApi.mockGenerateScheduleApi(cleanFormData);
      } else {
        // 如果禁用了模拟回退，则抛出原始错误，便于调试
        console.error('API调用失败且模拟回退已禁用:', error);
        throw error;
      }
    }
  } catch (error) {
    console.error('生成排课方案失败:', error);
    throw error;
  }
};

// 获取排课历史记录
export const getScheduleHistory = async (semesterId) => {
  try {
    const results = await apiRequest(`/Scheduling/history/${semesterId}`, 'GET');
    
    // 将后端返回的结果转换为前端期望的格式
    return results.map(result => ({
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
    }));
  } catch (error) {
    console.error('获取排课历史记录失败:', error);
    throw error;
  }
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
    console.error('获取排课方案失败:', error);
    throw error;
  }
};

// 发布排课方案
export const publishSchedule = async (scheduleId) => {
  try {
    return await apiRequest(`/Scheduling/publish/${scheduleId}`, 'PUT');
  } catch (error) {
    console.error('发布排课方案失败:', error);
    throw error;
  }
};

// 取消排课方案
export const cancelSchedule = async (scheduleId) => {
  try {
    return await apiRequest(`/Scheduling/cancel/${scheduleId}`, 'PUT');
  } catch (error) {
    console.error('取消排课方案失败:', error);
    throw error;
  }
};

// 获取时间槽数据
export const getTimeSlotsApi = async () => {
  try {
    const timeSlots = await apiRequest('/TimeSlot', 'GET');
    return timeSlots;
  } catch (error) {
    console.error('获取时间槽数据失败:', error);
    throw error;
  }
};
