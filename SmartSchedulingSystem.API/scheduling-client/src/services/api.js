// Import mock data for fallback when API fails
import { mockScheduleResults, mockSemesters, mockCourses, mockTeachers, mockClassrooms } from './mockData';

// API service - responsible for all communication with backend API
const API_BASE_URL = '/api'; // Use relative path to avoid CORS issues

// Define available API endpoint types
export const API_ENDPOINTS = {
  MOCK: 'mock',
  TEST_MOCK: 'test_mock',
  SCHEDULE_BASIC: 'schedule_basic',
  SCHEDULE_ADVANCED: 'schedule_advanced',
  SCHEDULE_ENHANCED: 'schedule_enhanced',
};

// Generic API request function
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
      // Try to get error message from API response
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

// Mock API service
const mockApi = {
  // Mock generate schedule API
  mockGenerateScheduleApi: async (formData) => {
    console.log('Using mock API to generate schedule', formData);
    
    // Simulate processing delay
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // Generate response using mockData
    const semester = mockSemesters.find(s => s.id === formData.semester) || mockSemesters[0];
    const scheduleResult = mockScheduleResults[0];
    
    // Create 3 schedule variants (if multiple solutions requested)
    const schedules = [];
    const solutionCount = formData.generateMultipleSolutions ? (formData.solutionCount || 3) : 1;
    
    for (let i = 1; i <= solutionCount; i++) {
      const scheduleId = 1000 + i;
      schedules.push({
        id: scheduleId,
        name: `${semester.name} Plan ${i}`,
        createdAt: new Date().toISOString(),
        status: 'Draft',
        score: 0.8 - (i - 1) * 0.1, // Make each subsequent plan slightly lower score
        details: scheduleResult.details.map(detail => ({
          ...detail,
          // Slightly modify each plan to make them different
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

// Generate schedule API
export const generateScheduleApi = async (formData) => {
  try {
    // Get debug parameters and API endpoint type from request
    const debugOptions = formData._debug || {};
    const apiEndpointType = formData.apiEndpointType || API_ENDPOINTS.TEST_MOCK;
    
    // Whether to enable mock mode (set to false to disable mock fallback)
    const enableMockFallback = !debugOptions.disableMockFallback;
    // Whether to enable debug mode (print more logs)
    const verboseLogging = debugOptions.verboseLogging;

    // Clean data sent to backend, remove debug parameters and API endpoint type
    const cleanFormData = { ...formData };
    delete cleanFormData._debug;
    delete cleanFormData.apiEndpointType;

    // Get complete object data from frontend cached data
    const allCourseSections = window.globalCourseData || [];
    const allTeachers = window.globalTeacherData || [];
    const allClassrooms = window.globalClassroomData || [];
    const allTimeSlots = window.globalTimeSlotData || [];
    
    // Find complete objects by ID
    const selectedCourseSectionObjects = (cleanFormData.courses || []).map(id => {
      // First look in global data
      let courseObj = allCourseSections.find(c => c.id === id);
      
      // If not found, look in mock data
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
          // If not found in both places, use default values
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
      // First look in global data
      let teacherObj = allTeachers.find(t => t.id === id);
      
      // If not found, look in mock data
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
          // If not found in both places, use default values
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
      // First look in global data
      let classroomObj = allClassrooms.find(c => c.id === id);
      
      // If not found, look in mock data
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
          // If not found in both places, use default values
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

    // Add default time slot data
    const timeSlotObjects = [];
    if (allTimeSlots && allTimeSlots.length > 0) {
      // Use global time slot data
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

    // Convert frontend formData to backend ScheduleRequestDto format
    const scheduleRequest = {
      // Keep original field names while mapping to backend expected field names
      semesterId: cleanFormData.semester,
      // Provide both IDs and complete objects for compatibility
      courseSectionIds: cleanFormData.courses || [],
      teacherIds: cleanFormData.teachers || [],
      classroomIds: cleanFormData.classrooms || [],
      // Keep original field names for compatibility
      courses: cleanFormData.courses || [],
      teachers: cleanFormData.teachers || [],
      classrooms: cleanFormData.classrooms || [],
      // Add possible time slot ID list
      timeSlotIds: cleanFormData.timeSlots || [],
      timeSlots: cleanFormData.timeSlots || [], // Add this field for compatibility
      
      // Add complete object data (newly added)
      courseSectionObjects: selectedCourseSectionObjects,
      teacherObjects: selectedTeacherObjects,
      classroomObjects: selectedClassroomObjects,
      timeSlotObjects: timeSlotObjects,
      
      // Add organizational scope parameters
      schedulingScope: cleanFormData.schedulingScope || 'programme',
      campusId: cleanFormData.campus || null,
      schoolId: cleanFormData.school || null,
      departmentId: cleanFormData.department || null,
      programmeId: cleanFormData.programme || null,
      // Keep original field names
      campus: cleanFormData.campus || null,
      school: cleanFormData.school || null,
      department: cleanFormData.department || null,
      subject: cleanFormData.subject || null,
      programme: cleanFormData.programme || null,
      
      // Add scheduling parameters
      facultyWorkloadBalance: cleanFormData.facultyWorkloadBalance || 0.8,
      studentScheduleCompactness: cleanFormData.studentScheduleCompactness || 0.7,
      classroomTypeMatchingWeight: cleanFormData.classroomTypeMatchingWeight || 0.7,
      minimumTravelTime: cleanFormData.minimumTravelTime || 30,
      maximumConsecutiveClasses: cleanFormData.maximumConsecutiveClasses || 3,
      campusTravelTimeWeight: cleanFormData.campusTravelTimeWeight || 0.6,
      preferredClassroomProximity: cleanFormData.preferredClassroomProximity || 0.5,
      
      // Other parameters
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

    // Try to call backend API
    try {
      let response;
      
      // Call different API based on selected endpoint type
      switch (apiEndpointType) {
        case API_ENDPOINTS.MOCK:
          // Use mock scheduling API instead of real scheduling API
          console.log('Using mock scheduling API...');
          response = await apiRequest('/Scheduling/generate', 'POST', scheduleRequest);
          break;
          
        case API_ENDPOINTS.TEST_MOCK:
          // Use test controller's mock-schedule endpoint, uses random algorithm for scheduling, prone to conflicts, good for testing LLM conflict analysis
          console.log('Using test controller\'s mock-schedule endpoint...');
          response = await apiRequest('/Test/mock-schedule', 'POST', scheduleRequest);
          break;
          
        case API_ENDPOINTS.SCHEDULE_BASIC:
          // Use level 1 constraints
          console.log('Using Schedule controller\'s generate endpoint with level 1 constraints...');
          response = await apiRequest('/Schedule/generate', 'POST', scheduleRequest);
          break;
          
        case API_ENDPOINTS.SCHEDULE_ADVANCED:
          // Use level 2 constraints, adds two types of availability constraints
          console.log('Using Schedule controller\'s generate-advanced endpoint with level 2 constraints...');
          response = await apiRequest('/Schedule/generate-advanced', 'POST', scheduleRequest);
          break;
          
        case API_ENDPOINTS.SCHEDULE_ENHANCED:
          // Use level 3 constraints, adds classroom resource constraints and (course-classroom) type matching constraints
          console.log('Using Schedule controller\'s generate-enhanced endpoint with level 3 constraints...');
          response = await apiRequest('/Schedule/generate-enhanced', 'POST', scheduleRequest);
          break;
          
        default:
          // Default to test controller's mock-schedule endpoint
          console.log('Using default (test controller\'s mock-schedule) endpoint...');
          response = await apiRequest('/Test/mock-schedule', 'POST', scheduleRequest);
      }

      console.log('Schedule generation successful:', response);
      return response;
    } catch (apiError) {
      console.error('Schedule API call failed:', apiError);
      
      // If mock fallback is enabled, use mock data when real API fails
      if (enableMockFallback) {
        console.warn('Fallback to mock data...');
        return await mockApi.mockGenerateScheduleApi(cleanFormData);
      } else {
        // Otherwise, throw the error up
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
    // If the API fails, return simulated data as a fallback scenario
    return generateMockScheduleHistory(semesterId, limit);
  }
};

// Generate mock schedule history
const generateMockScheduleHistory = (semesterId, limit = 10) => {
  const mockHistory = [];
  
  // Generate the latest 10 schedule records
  for (let i = 0; i < limit; i++) {
    const requestId = 1000 - i;
    const date = new Date();
    date.setDate(date.getDate() - i * 2); // Every two days a record
    
    // Generate 1-3 schedule solutions for each request
    const solutionCount = Math.floor(Math.random() * 3) + 1;
    const schedules = [];
    
    for (let j = 0; j < solutionCount; j++) {
      // Generate random status, newer records are more likely to be Draft status
      let status = 'Draft';
      if (i > 2) {
        const statuses = ['Draft', 'Published', 'Canceled', 'Archived'];
        const statusIndex = Math.floor(Math.random() * statuses.length);
        status = statuses[statusIndex];
      }
      
      // Generate status history
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

  // Generate mock schedule details
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
    const dayIndex = Math.floor(Math.random() * 5) + 1; // Monday to Friday
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

// Get scheduling plan based on ID
export const getScheduleById = async (scheduleId) => {
  try {
    const result = await apiRequest(`/Scheduling/${scheduleId}`, 'GET');
    
    // Convert the backend result to the frontend expected format
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

// Publish schedule
export const publishSchedule = async (scheduleId) => {
  try {
    return await apiRequest(`/Scheduling/publish/${scheduleId}`, 'PUT');
  } catch (error) {
    console.error('Failed to publish schedule:', error);
    throw error;
  }
};

// Cancel schedule
export const cancelSchedule = async (scheduleId) => {
  try {
    return await apiRequest(`/Scheduling/cancel/${scheduleId}`, 'PUT');
  } catch (error) {
    console.error('Failed to cancel schedule:', error);
    throw error;
  }
};

// Get time slot data
export const getTimeSlotsApi = async () => {
  try {
    const timeSlots = await apiRequest('/TimeSlot', 'GET');
    return timeSlots;
  } catch (error) {
    console.error('Failed to get time slot data:', error);
    throw error;
  }
};
