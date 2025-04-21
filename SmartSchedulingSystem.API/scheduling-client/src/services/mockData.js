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
  { id: 4, code: 'CS401', name: 'Artificial Intelligence', department: 'Computer Science', enrollment: 45, subjectId: 1 },
  { id: 5, code: 'CS501', name: 'Computer Networks', department: 'Computer Science', enrollment: 70, subjectId: 1 },
  { id: 6, code: 'MATH101', name: 'Advanced Mathematics', department: 'Mathematics', enrollment: 150, subjectId: 2 },
  { id: 7, code: 'MATH201', name: 'Linear Algebra', department: 'Mathematics', enrollment: 100, subjectId: 2 },
  { id: 8, code: 'MATH301', name: 'Calculus III', department: 'Mathematics', enrollment: 80, subjectId: 2 },
  { id: 9, code: 'PHYS101', name: 'Physics I', department: 'Physics', enrollment: 100, subjectId: 3 },
  { id: 10, code: 'PHYS201', name: 'Physics II', department: 'Physics', enrollment: 85, subjectId: 3 },
  { id: 11, code: 'FIN101', name: 'Financial Accounting', department: 'Finance', enrollment: 120, subjectId: 4 },
  { id: 12, code: 'MKT101', name: 'Marketing Principles', department: 'Marketing', enrollment: 110, subjectId: 5 },
  { id: 13, code: 'ECON101', name: 'Introduction to Economics', department: 'Economics', enrollment: 100, subjectId: 8 },
  { id: 14, code: 'BUS201', name: 'Business Administration', department: 'Business', enrollment: 100, subjectId: 9 },
  { id: 15, code: 'CS601', name: 'Evening Programming Lab', department: 'Computer Science', enrollment: 50, subjectId: 1 },
  { id: 16, code: 'CS701', name: 'Advanced Programming Techniques', department: 'Computer Science', enrollment: 50, subjectId: 1 },
];
  
  // 在services/mockData.js中添加教师学科关系
  export const mockTeacherSubjects = [
    { teacherId: 1, subjectId: 1 }, // Prof. Smith - Computer Science
    { teacherId: 2, subjectId: 1 }, // Prof. Johnson - Computer Science
    { teacherId: 3, subjectId: 1 }, // Prof. Davis - Computer Science
    { teacherId: 4, subjectId: 1 }, // Prof. Wilson - Computer Science
    { teacherId: 5, subjectId: 2 }, // Prof. Williams - Mathematics
    { teacherId: 6, subjectId: 2 }, // Prof. Taylor - Mathematics
    { teacherId: 7, subjectId: 3 }, // Prof. Brown - Physics
    { teacherId: 8, subjectId: 3 }, // Prof. Miller - Physics
    { teacherId: 9, subjectId: 4 }, // Prof. Anderson - Finance
    { teacherId: 10, subjectId: 5 } // Prof. Thomas - Marketing
  ];

  // 修改mockTeachers，添加departmentId字段
  export const mockTeachers = [
    { id: 1, name: 'Prof. Smith', code: 'SMITH', department: 'Computer Science', departmentId: 1 },
    { id: 2, name: 'Prof. Johnson', code: 'JOHN', department: 'Computer Science', departmentId: 1 },
    { id: 3, name: 'Prof. Davis', code: 'DAVIS', department: 'Computer Science', departmentId: 1 },
    { id: 4, name: 'Prof. Wilson', code: 'WILS', department: 'Computer Science', departmentId: 1 },
    { id: 5, name: 'Prof. Williams', code: 'WILL', department: 'Mathematics', departmentId: 5 },
    { id: 6, name: 'Prof. Taylor', code: 'TAYL', department: 'Mathematics', departmentId: 5 },
    { id: 7, name: 'Prof. Brown', code: 'BROWN', department: 'Physics', departmentId: 6 },
    { id: 8, name: 'Prof. Miller', code: 'MILL', department: 'Physics', departmentId: 6 },
    { id: 9, name: 'Prof. Anderson', code: 'ANDR', department: 'Finance', departmentId: 4 },
    { id: 10, name: 'Prof. Thomas', code: 'THOM', department: 'Marketing', departmentId: 3 }
  ];
  
  export const mockClassrooms = [
    { id: 1, name: '101', building: 'Building A', capacity: 120, hasComputers: true, type: 'ComputerLab', campusId: 1 },
    { id: 2, name: '102', building: 'Building A', capacity: 100, hasComputers: true, type: 'ComputerLab', campusId: 1 },
    { id: 3, name: '201', building: 'Building A', capacity: 80, hasComputers: false, type: 'Lecture', campusId: 1 },
    { id: 4, name: '202', building: 'Building A', capacity: 90, hasComputers: false, type: 'Lecture', campusId: 1 },
    { id: 5, name: '301', building: 'Building B', capacity: 150, hasComputers: false, type: 'LargeHall', campusId: 2 },
    { id: 6, name: '302', building: 'Building B', capacity: 140, hasComputers: false, type: 'LargeHall', campusId: 2 },
    { id: 7, name: '401', building: 'Building B', capacity: 60, hasComputers: true, type: 'Laboratory', campusId: 2 },
    { id: 8, name: '402', building: 'Building B', capacity: 65, hasComputers: true, type: 'Laboratory', campusId: 2 },
    { id: 9, name: '501', building: 'Building C', capacity: 100, hasComputers: false, type: 'Lecture', campusId: 1 },
    { id: 10, name: '601', building: 'Building C', capacity: 80, hasComputers: true, type: 'ComputerLab', campusId: 1 }
  ];
  
  export const mockConstraints = [
    { id: 1, name: 'Teacher Availability', type: 'Hard', description: 'Teachers must be available during scheduled time slots', weight: 1.0, isActive: true },
    { id: 2, name: 'Classroom Capacity', type: 'Hard', description: 'Classroom capacity must meet course enrollment needs', weight: 1.0, isActive: true },
    { id: 3, name: 'Consecutive Teaching', type: 'Soft', description: 'Try to schedule consecutive classes for teachers on same day', weight: 0.8, isActive: true },
    { id: 4, name: 'Classroom Type Match', type: 'Soft', description: 'Match courses with appropriate classroom types', weight: 0.7, isActive: true }
  ];
  
  // 添加一个辅助函数来获取星期几的名称
  function getDayName(day) {
    const dayNames = ['', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    return dayNames[day] || 'Unknown';
  }
  
  // 修改时间槽模拟数据，采用GenerateStandardTimeSlots风格的时间段
  export const mockTimeSlots = (() => {
    const slots = [];
    let slotId = 1;
    
    // 周一到周日
    for (let day = 1; day <= 7; day++) {
      // 修复dayNames数组，使索引与day值匹配
      const dayNames = ['', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
      const dayName = dayNames[day];
      
      // Morning slots (8:00 - 12:00)
      slots.push({
        id: slotId++,
        dayOfWeek: day,
        dayName,
        startTime: '08:00',
        endTime: '09:30',
        type: 'Morning'
      });
      
      slots.push({
        id: slotId++,
        dayOfWeek: day,
        dayName,
        startTime: '10:00',
        endTime: '11:30',
        type: 'Morning'
      });
      
      // Afternoon slots (14:00 - 18:00)
      slots.push({
        id: slotId++,
        dayOfWeek: day,
        dayName,
        startTime: '14:00',
        endTime: '15:30',
        type: 'Afternoon'
      });
      
      slots.push({
        id: slotId++,
        dayOfWeek: day,
        dayName,
        startTime: '16:00',
        endTime: '17:30',
        type: 'Afternoon'
      });
      
      // Evening slots (19:00 - 22:30)
      slots.push({
        id: slotId++,
        dayOfWeek: day,
        dayName,
        startTime: '19:00',
        endTime: '20:30',
        type: 'Evening'
      });
      
      // 添加第二个晚上时间段，与第一个不重叠
      slots.push({
        id: slotId++,
        dayOfWeek: day,
        dayName,
        startTime: '21:00',
        endTime: '22:30',
        type: 'Evening'
      });
    }
    
    return slots;
  })();
  
  // 添加教师可用性和教室可用性数据
  export const mockTeacherAvailabilities = (() => {
    const availabilities = [];
    
    // 所有教师在所有时间槽都可用
    mockTeachers.forEach(teacher => {
      mockTimeSlots.forEach(timeSlot => {
        availabilities.push({
          teacherId: teacher.id,
          timeSlotId: timeSlot.id,
          isAvailable: true
        });
      });
    });
    
    return availabilities;
  })();

  export const mockClassroomAvailabilities = (() => {
    const availabilities = [];
    
    // 所有教室在所有时间槽都可用
    mockClassrooms.forEach(classroom => {
      mockTimeSlots.forEach(timeSlot => {
        availabilities.push({
          classroomId: classroom.id,
          timeSlotId: timeSlot.id,
          isAvailable: true
        });
      });
    });
    
    return availabilities;
  })();
  
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
        },
        {
          courseCode: 'CS401',
          courseName: 'Software Engineering Workshop',
          teacherName: 'Prof. Davis',
          classroom: 'Building A-305',
          day: 1,
          dayName: 'Monday',
          startTime: '19:00',
          endTime: '20:30'
        },
        {
          courseCode: 'BUS201',
          courseName: 'Business Administration',
          teacherName: 'Prof. Taylor',
          classroom: 'Building C-101',
          day: 2,
          dayName: 'Tuesday',
          startTime: '19:00',
          endTime: '20:30'
        },
        {
          courseCode: 'CS601',
          courseName: 'Evening Programming Lab',
          teacherName: 'Prof. Anderson',
          classroom: 'Building A-401',
          day: 4,
          dayName: 'Thursday',
          startTime: '21:00',
          endTime: '22:30'
        }
      ]
    },
    {
      id: 102,
      name: 'Spring 2025 Schedule v2',
      createdAt: '2025-01-16T14:45:00',
      status: 'Draft',
      semesterName: 'Summer 2025',
      details: [
        {
          courseCode: 'CS101',
          courseName: 'Introduction to Computer Science',
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
          day: 3,
          dayName: 'Wednesday',
          startTime: '08:00',
          endTime: '09:30'
        },
        {
          courseCode: 'CS301',
          courseName: 'Algorithm Design',
          teacherName: 'Prof. Smith',
          classroom: 'Building A-101',
          day: 2,
          dayName: 'Tuesday',
          startTime: '14:00',
          endTime: '15:30'
        },
        {
          courseCode: 'PSYCH101',
          courseName: 'Introduction to Psychology',
          teacherName: 'Prof. Martinez',
          classroom: 'Building D-201',
          day: 2,
          dayName: 'Tuesday',
          startTime: '19:00',
          endTime: '20:30'
        },
        {
          courseCode: 'LANG101',
          courseName: 'Foreign Language Studies',
          teacherName: 'Prof. Chen',
          classroom: 'Building C-202',
          day: 3,
          dayName: 'Wednesday',
          startTime: '19:00',
          endTime: '20:30'
        },
        {
          courseCode: 'MUS101',
          courseName: 'Music Appreciation',
          teacherName: 'Prof. Garcia',
          classroom: 'Building E-101',
          day: 4,
          dayName: 'Thursday',
          startTime: '21:00',
          endTime: '22:30'
        },
        {
          courseCode: 'ART101',
          courseName: 'Introduction to Fine Arts',
          teacherName: 'Prof. Lee',
          classroom: 'Building E-202',
          day: 5,
          dayName: 'Friday',
          startTime: '19:00',
          endTime: '20:30'
        }
      ]
    },
    {
      id: 103,
      name: 'Evening Program - Fall 2025',
      createdAt: '2025-06-20T09:15:00',
      status: 'Published',
      semesterName: 'Fall 2025',
      details: [
        {
          courseCode: 'CS501',
          courseName: 'Advanced Programming Techniques',
          teacherName: 'Prof. Wilson',
          classroom: 'Building A-301',
          day: 1,
          dayName: 'Monday',
          startTime: '19:00',
          endTime: '20:30'
        },
        {
          courseCode: 'ECON101',
          courseName: 'Introduction to Economics',
          teacherName: 'Prof. Evans',
          classroom: 'Building C-301',
          day: 2,
          dayName: 'Tuesday',
          startTime: '19:00',
          endTime: '20:30'
        },
        {
          courseCode: 'PHIL201',
          courseName: 'Critical Thinking',
          teacherName: 'Prof. Nelson',
          classroom: 'Building D-101',
          day: 3,
          dayName: 'Wednesday',
          startTime: '19:00',
          endTime: '20:30'
        },
        {
          courseCode: 'BUS201',
          courseName: 'Business Administration',
          teacherName: 'Prof. Baker',
          classroom: 'Building C-201',
          day: 4,
          dayName: 'Thursday',
          startTime: '21:00',
          endTime: '22:30'
        },
        {
          courseCode: 'CS601',
          courseName: 'Evening Programming Lab',
          teacherName: 'Prof. Thompson',
          classroom: 'Building A-401',
          day: 5,
          dayName: 'Friday',
          startTime: '19:00',
          endTime: '20:30'
        }
      ]
    }
  ];
  
  // Function to simulate API call for schedule generation
  // In generateScheduleApi function in mockData.js, modify to generate multiple schedules
  export const generateScheduleApi = (formData) => {
    return new Promise((resolve, reject) => {
      // 基本验证
      if (!formData) {
        reject(new Error('没有提供表单数据'));
        return;
      }

      if (!formData.semester) {
        reject(new Error('必须选择学期'));
        return;
      }

      if (!formData.courses || formData.courses.length === 0) {
        reject(new Error('必须选择至少一个课程'));
        return;
      }

      if (!formData.teachers || formData.teachers.length === 0) {
        reject(new Error('必须选择至少一个教师'));
        return;
      }

      if (!formData.classrooms || formData.classrooms.length === 0) {
        reject(new Error('必须选择至少一个教室'));
        return;
      }

      // 模拟API调用延迟
      setTimeout(() => {
        try {
          console.log('处理排课请求:', formData);
          
          // 检查是否可以找到所选的课程、教师和教室
          const selectedCourses = formData.courses.map(id => 
            mockCourses.find(c => c.id === id)
          ).filter(Boolean);
          
          const selectedTeachers = formData.teachers.map(id => 
            mockTeachers.find(t => t.id === id)
          ).filter(Boolean);
          
          const selectedClassrooms = formData.classrooms.map(id => 
            mockClassrooms.find(c => c.id === id)
          ).filter(Boolean);
          
          if (selectedCourses.length === 0) {
            reject(new Error('找不到所选课程'));
            return;
          }
          
          if (selectedTeachers.length === 0) {
            reject(new Error('找不到所选教师'));
            return;
          }
          
          if (selectedClassrooms.length === 0) {
            reject(new Error('找不到所选教室'));
            return;
          }
          
          // 创建排课结果
          const semester = mockSemesters.find(s => s.id === formData.semester);
          if (!semester) {
            reject(new Error('找不到所选学期'));
            return;
          }
          
          // Generate multiple schedules if requested
          if (formData.generateAlternatives) {
            // Create 3 alternative schedules
            const schedules = [];
            for (let i = 1; i <= 3; i++) {
              schedules.push({
                id: 103 + i,
                name: `${semester.name} Alternative ${i}`,
                createdAt: new Date().toISOString(),
                status: 'Draft',
                semesterName: semester.name,
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
                name: `${semester.name} New Schedule`,
                createdAt: new Date().toISOString(),
                status: 'Draft',
                semesterName: semester.name,
                details: mockScheduleResults[0].details
              }],
              primaryScheduleId: 103
            });
          }
        } catch (error) {
          console.error('生成排课时发生错误:', error);
          reject(new Error('生成排课时发生内部错误: ' + (error.message || '未知错误')));
        }
      }, 2000);
    });
  };

