// src/services/api.js
import axios from 'axios';

const api = axios.create({
    baseURL: 'http://localhost:5192/api',
    headers: {
        'Content-Type': 'application/json'
    }
});

// 添加响应拦截器
api.interceptors.response.use(
    response => response,
    error => {
        console.error('API Error:', error);

        // 提取错误信息
        const errorMessage = error.response?.data?.message ||
            error.response?.data?.detailedMessage ||
            'Unknown error occurred';

        // 这里可以调用通知系统显示错误
        // toast.error(errorMessage);

        return Promise.reject(error);
    }
);

// 瀛︽湡API
export const semesterApi = {
    getAllSemesters: () => api.get('/semesters'),
    getSemesterById: (id) => api.get(`/semesters/${id}`),
    createSemester: (data) => api.post('/semesters', data),
    updateSemester: (data) => api.put(`/semesters/${data.semesterId}`, data),
    deleteSemester: (id) => api.delete(`/semesters/${id}`)
};

// 鏁欏笀API
export const teacherApi = {
    getAllTeachers: () => api.get('/teachers'),
    getTeachersByDepartment: (departmentId) => api.get(`/teachers/department/${departmentId}`),
    getTeacherById: (id) => api.get(`/teachers/${id}`),
    createTeacher: (data) => api.post('/teachers', data),
    updateTeacher: (data) => api.put(`/teachers/${data.teacherId}`, data),
    deleteTeacher: (id) => api.delete(`/teachers/${id}`),
    getTeacherAvailability: (teacherId) => api.get(`/teachers/${teacherId}/availability`),
    updateTeacherAvailability: (teacherId, data) => api.put(`/teachers/${teacherId}/availability`, data)
};

// 鏁欏API
export const classroomApi = {
    getAllClassrooms: () => api.get('/classrooms'),
    getClassroomById: (id) => api.get(`/classrooms/${id}`),
    createClassroom: (data) => api.post('/classrooms', data),
    updateClassroom: (data) => api.put(`/classrooms/${data.classroomId}`, data),
    deleteClassroom: (id) => api.delete(`/classrooms/${id}`),
    getClassroomAvailability: (classroomId) => api.get(`/classrooms/${classroomId}/availability`),
    updateClassroomAvailability: (classroomId, data) => api.put(`/classrooms/${classroomId}/availability`, data)
};

// 璇剧▼鐝骇API
export const courseSectionApi = {
    getAllCourseSections: () => api.get('/coursesections'),
    getCourseSectionsBySemester: (semesterId) => api.get(`/coursesections/semester/${semesterId}`),
    getCourseSectionsByCourse: (courseId) => api.get(`/coursesections/course/${courseId}`),
    getCourseSectionById: (id) => api.get(`/coursesections/${id}`),
    createCourseSection: (data) => api.post('/coursesections', data),
    updateCourseSection: (data) => api.put(`/coursesections/${data.courseSectionId}`, data),
    deleteCourseSection: (id) => api.delete(`/coursesections/${id}`)
};

// 鏃堕棿娈礎PI
export const timeSlotApi = {
    getAllTimeSlots: () => api.get('/timeslots'),
    getTimeSlotById: (id) => api.get(`/timeslots/${id}`),
    createTimeSlot: (data) => api.post('/timeslots', data),
    updateTimeSlot: (data) => api.put(`/timeslots/${data.timeSlotId}`, data),
    deleteTimeSlot: (id) => api.delete(`/timeslots/${id}`)
};

// 绾︽潫鏉′欢API
export const constraintApi = {
    getAllConstraints: () => api.get('/constraints'),
    getConstraintById: (id) => api.get(`/constraints/${id}`),
    createConstraint: (data) => api.post('/constraints', data),
    updateConstraint: (data) => api.put(`/constraints/${data.constraintId}`, data),
    deleteConstraint: (id) => api.delete(`/constraints/${id}`),
    updateConstraintsSettings: (data) => api.put('/constraints/settings', data)
};

// 鎺掕API
export const scheduleApi = {
    generateSchedule: (data) => {
        console.log('API Request Data:', JSON.stringify(data, null, 2));
        return api.post('/scheduling/generate', data);
    },
    getScheduleHistory: async (semesterId, queryParams = '') => {
        try {
            console.log(`API call: 获取学期 ${semesterId} 的排课历史`, queryParams ? `(带筛选条件: ${queryParams})` : '');
            // 拼接查询参数
            const url = queryParams
                ? `/scheduling/history/${semesterId}?${queryParams}`
                : `/scheduling/history/${semesterId}`;

            const response = await api.get(url);
            console.log('API response:', response.data);
            return response;
        } catch (error) {
            console.error(`获取学期 ${semesterId} 的排课历史失败:`, error);
            throw error;
        }
    },
    getScheduleById: (scheduleId) => api.get(`/scheduling/${scheduleId}`),
    publishSchedule: (scheduleId) => api.put(`/scheduling/publish/${scheduleId}`),
    cancelSchedule: (scheduleId) => api.put(`/scheduling/cancel/${scheduleId}`)
};
// 课程API
export const courseApi = {
    getAllCourses: () => api.get('/courses'),
    getCoursesByDepartment: (departmentId) => api.get(`/courses/department/${departmentId}`),
    getCourseById: (id) => api.get(`/courses/${id}`),
    createCourse: (data) => api.post('/courses', data),
    updateCourse: (data) => api.put(`/courses/${data.courseId}`, data),
    deleteCourse: (id) => api.delete(`/courses/${id}`),
    getCoursePrerequisites: (courseId) => api.get(`/courses/${courseId}/prerequisites`)
};

// 涓轰簡鍚戝悗鍏煎锛屼繚鐣欏師鏉ョ殑鍑芥暟
export const getScheduleData = () => api.get('/scheduling/data');

export default api;

