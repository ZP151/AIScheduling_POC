// Mock data for development
export const mockSemesters = [
    { id: 1, name: 'Spring 2025' },
    { id: 2, name: 'Summer 2025' },
    { id: 3, name: 'Fall 2025' }
  ];
  // Add these to mockData.js or create a new file for organizational mock data

export const mockCampuses = [
  { id: 1, name: 'Main Campus' },
  { id: 2, name: 'Downtown Campus' },
  { id: 3, name: 'Medical Campus' }
];

export const mockSchools = [
  { id: 1, name: 'School of Engineering', campusId: 1 },
  { id: 2, name: 'School of Business', campusId: 1 },
  { id: 3, name: 'School of Arts and Sciences', campusId: 2 },
  { id: 4, name: 'School of Medicine', campusId: 3 }
];

export const mockDepartments = [
  { id: 1, name: 'Computer Science', schoolId: 1 },
  { id: 2, name: 'Electrical Engineering', schoolId: 1 },
  { id: 3, name: 'Marketing', schoolId: 2 },
  { id: 4, name: 'Finance', schoolId: 2 },
  { id: 5, name: 'Mathematics', schoolId: 3 },
  { id: 6, name: 'Physics', schoolId: 3 },
  { id: 7, name: 'Clinical Medicine', schoolId: 4 }
];

export const mockProgrammes = [
  { id: 1, name: 'BS Computer Science', departmentId: 1 },
  { id: 2, name: 'MS Computer Science', departmentId: 1 },
  { id: 3, name: 'BS Electrical Engineering', departmentId: 2 },
  { id: 4, name: 'MBA Marketing', departmentId: 3 },
  { id: 5, name: 'BS Finance', departmentId: 4 },
  { id: 6, name: 'BS Mathematics', departmentId: 5 },
  { id: 7, name: 'BS Physics', departmentId: 6 },
  { id: 8, name: 'MD Clinical Medicine', departmentId: 7 }
];

// 在services/mockData.js中添加Subject数据
export const mockSubjects = [
  { id: 1, name: 'Computer Science', code: 'CS', departmentId: 1 },
  { id: 2, name: 'Mathematics', code: 'MATH', departmentId: 5 },
  { id: 3, name: 'Physics', code: 'PHYS', departmentId: 6 },
  { id: 4, name: 'Finance', code: 'FIN', departmentId: 4 },
  { id: 5, name: 'Marketing', code: 'MKT', departmentId: 3 }
];

  // 修改mockCourses，添加subjectId字段关联Subject
export const mockCourses = [
  { id: 1, code: 'CS101', name: 'Introduction to Computer Science', department: 'Computer Science', enrollment: 120, subjectId: 1 },
  { id: 2, code: 'CS201', name: 'Data Structures', department: 'Computer Science', enrollment: 90, subjectId: 1 },
  { id: 3, code: 'CS301', name: 'Algorithm Design', department: 'Computer Science', enrollment: 60, subjectId: 1 },
  { id: 4, code: 'MATH101', name: 'Advanced Mathematics', department: 'Mathematics', enrollment: 150, subjectId: 2 },
  { id: 5, code: 'PHYS101', name: 'Physics I', department: 'Physics', enrollment: 100, subjectId: 3 }
];
  
  // 在services/mockData.js中添加教师学科关系
  export const mockTeacherSubjects = [
    { teacherId: 1, subjectId: 1 }, // Prof. Smith 可以教授 Computer Science
    { teacherId: 2, subjectId: 1 }, // Prof. Johnson 可以教授 Computer Science
    { teacherId: 3, subjectId: 2 }, // Prof. Williams 可以教授 Mathematics
    { teacherId: 4, subjectId: 3 }  // Prof. Brown 可以教授 Physics
  ];

  // 修改mockTeachers，添加departmentId字段
  export const mockTeachers = [
    { id: 1, name: 'Prof. Smith', department: 'Computer Science', departmentId: 1 },
    { id: 2, name: 'Prof. Johnson', department: 'Computer Science', departmentId: 1 },
    { id: 3, name: 'Prof. Williams', department: 'Mathematics', departmentId: 5 },
    { id: 4, name: 'Prof. Brown', department: 'Physics', departmentId: 6 }
  ];
  
  export const mockClassrooms = [
    { id: 1, name: '101', building: 'Building A', capacity: 120, hasComputers: true, type: 'ComputerLab' },
    { id: 2, name: '201', building: 'Building A', capacity: 80, hasComputers: false, type: 'Lecture' },
    { id: 3, name: '301', building: 'Building B', capacity: 150, hasComputers: false, type: 'LargeHall' },
    { id: 4, name: '401', building: 'Building B', capacity: 60, hasComputers: true, type: 'Laboratory' }
  ];
  
  export const mockConstraints = [
    { id: 1, name: 'Teacher Availability', type: 'Hard', description: 'Teachers must be available during scheduled time slots', weight: 1.0, isActive: true },
    { id: 2, name: 'Classroom Capacity', type: 'Hard', description: 'Classroom capacity must meet course enrollment needs', weight: 1.0, isActive: true },
    { id: 3, name: 'Consecutive Teaching', type: 'Soft', description: 'Try to schedule consecutive classes for teachers on same day', weight: 0.8, isActive: true },
    { id: 4, name: 'Classroom Type Match', type: 'Soft', description: 'Match courses with appropriate classroom types', weight: 0.7, isActive: true }
  ];
  
  export const mockTimeSlots = [
    { id: 1, day: 1, dayName: 'Monday', startTime: '08:00', endTime: '09:30' },
    { id: 2, day: 1, dayName: 'Monday', startTime: '10:00', endTime: '11:30' },
    { id: 3, day: 2, dayName: 'Tuesday', startTime: '08:00', endTime: '09:30' },
    { id: 4, day: 2, dayName: 'Tuesday', startTime: '10:00', endTime: '11:30' },
    { id: 5, day: 3, dayName: 'Wednesday', startTime: '08:00', endTime: '09:30' }
  ];
  
  export const mockScheduleResults = [
    {
      id: 101,
      name: 'Spring 2025 Schedule v1',
      createdAt: '2025-01-15T10:30:00',
      status: 'Published',
      semesterName: 'Spring 2025', // 添加这个字段
      details: [
        {
          courseCode: 'CS101',
          courseName: 'Introduction to Computer Science',
          teacherName: 'Prof. Smith',
          classroom: 'Building A-101',
          day: 1,
          dayName: 'Monday',
          startTime: '08:00',
          endTime: '09:30'
        },
        {
          courseCode: 'CS201',
          courseName: 'Data Structures',
          teacherName: 'Prof. Johnson',
          classroom: 'Building A-201',
          day: 1,
          dayName: 'Monday',
          startTime: '10:00',
          endTime: '11:30'
        },
        {
          courseCode: 'MATH101',
          courseName: 'Advanced Mathematics',
          teacherName: 'Prof. Williams',
          classroom: 'Building B-301',
          day: 2,
          dayName: 'Tuesday',
          startTime: '08:00',
          endTime: '09:30'
        },
        {
          courseCode: 'PHYS101',
          courseName: 'Physics I',
          teacherName: 'Prof. Brown',
          classroom: 'Building B-301',
          day: 2,
          dayName: 'Tuesday',
          startTime: '10:00',
          endTime: '11:30'
        },
        {
          courseCode: 'CS301',
          courseName: 'Algorithm Design',
          teacherName: 'Prof. Smith',
          classroom: 'Building A-101',
          day: 3,
          dayName: 'Wednesday',
          startTime: '08:00',
          endTime: '09:30'
        }
      ]
    },
    {
      id: 102,
      name: 'Spring 2025 Schedule v2',
      createdAt: '2025-01-16T14:45:00',
      status: 'Draft',
      semesterName: 'Summer 2025', // 添加这个字段
      details: [
        // Similar structure to above but with different assignments
      ]
    }
  ];
  
  // Function to simulate API call for schedule generation
  // In generateScheduleApi function in mockData.js, modify to generate multiple schedules
  export const generateScheduleApi = (formData) => {
    return new Promise((resolve) => {
      setTimeout(() => {
        // Generate multiple schedules if requested
        if (formData.generateAlternatives) {
          // Create 3 alternative schedules
          const schedules = [];
          for (let i = 1; i <= 3; i++) {
            schedules.push({
              id: 103 + i,
              name: `${mockSemesters.find(s => s.id === formData.semester)?.name} Alternative ${i}`,
              createdAt: new Date().toISOString(),
              status: 'Draft',
              details: mockScheduleResults[0].details.map(detail => ({
                ...detail,
                // Slightly modify each schedule to make them different
                // This is just for demonstration - a real algorithm would create meaningful alternatives
                startTime: i === 1 ? detail.startTime : 
                          i === 2 ? (parseInt(detail.startTime.split(':')[0]) + 1).toString().padStart(2, '0') + ':00' :
                          (parseInt(detail.startTime.split(':')[0]) - 1).toString().padStart(2, '0') + ':00',
                endTime: i === 1 ? detail.endTime : 
                        i === 2 ? (parseInt(detail.endTime.split(':')[0]) + 1).toString().padStart(2, '0') + ':30' :
                        (parseInt(detail.endTime.split(':')[0]) - 1).toString().padStart(2, '0') + ':30'
              }))
            });
          }
          resolve({
            schedules: schedules,
            primaryScheduleId: 104 // Pick one as primary
          });
        } else {
          // Original single schedule behavior
          resolve({
            schedules: [{
              id: 103,
              name: `${mockSemesters.find(s => s.id === formData.semester)?.name} New Schedule`,
              createdAt: new Date().toISOString(),
              status: 'Draft',
              details: mockScheduleResults[0].details
            }],
            primaryScheduleId: 103
          });
        }
      }, 2000);
    });
  };

