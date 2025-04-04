// src/services/api.js
import axios from 'axios';

// 鍒涘缓axios瀹炰緥锛宐aseURL搴旇鎸囧悜鎮ㄧ殑.NET鍚庣API
const api = axios.create({
    baseURL: 'https://localhost:5192/api', // 鎴栬€呮偍鐨勫疄闄匒PI鍦板潃
    headers: {
        'Content-Type': 'application/json'
    }
});

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
    generateSchedule: (data) => api.post('/scheduling/generate', data),
    getScheduleHistory: (semesterId) => api.get(`/scheduling/history/${semesterId}`),
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

