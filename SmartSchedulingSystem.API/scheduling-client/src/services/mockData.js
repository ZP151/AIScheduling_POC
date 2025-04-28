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
    { id: 1, name: '101', building: 'Building A', capacity: 120, type: 'ComputerLab', features: ['Computers', 'Projector', 'Interactive Whiteboard'], campusId: 1 },
    { id: 2, name: '102', building: 'Building A', capacity: 100, type: 'ComputerLab', features: ['Computers', 'Projector'], campusId: 1 },
    { id: 3, name: '201', building: 'Building A', capacity: 80, type: 'Lecture', features: ['Projector', 'Whiteboard'], campusId: 1 },
    { id: 4, name: '202', building: 'Building A', capacity: 90, type: 'Lecture', features: ['Projector', 'Whiteboard'], campusId: 1 },
    { id: 5, name: '301', building: 'Building B', capacity: 150, type: 'LargeHall', features: ['Advanced Audio', 'Dual Projector'], campusId: 2 },
    { id: 6, name: '302', building: 'Building B', capacity: 140, type: 'LargeHall', features: ['Advanced Audio', 'Dual Projector'], campusId: 2 },
    { id: 7, name: '401', building: 'Building B', capacity: 60, type: 'Laboratory', features: ['Lab Equipment', 'Safety Facilities'], campusId: 2 },
    { id: 8, name: '402', building: 'Building B', capacity: 65, type: 'Laboratory', features: ['Lab Equipment', 'Safety Facilities'], campusId: 2 },
    { id: 9, name: '501', building: 'Building C', capacity: 100, type: 'Lecture', features: ['Projector', 'Whiteboard'], campusId: 1 },
    { id: 10, name: '601', building: 'Building C', capacity: 80, type: 'ComputerLab', features: ['Computers', 'Projector'], campusId: 1 }
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
  
  // 教室设备类型定义
  export const mockEquipmentTypes = [
    { id: 1, name: 'Projector', description: 'Projection device for slides and videos', movable: false },
    { id: 2, name: 'Computer', description: 'Desktop computers for teacher and students', movable: false },
    { id: 3, name: 'Interactive Whiteboard', description: 'Smart board supporting touch and electronic writing', movable: false },
    { id: 4, name: 'Teaching Podium', description: 'Podium for teachers', movable: false },
    { id: 5, name: 'Student Desks & Chairs', description: 'Desk and chair combination for students', movable: true },
    { id: 6, name: 'Lab Bench', description: 'Workbench in laboratory', movable: false },
    { id: 7, name: 'Power Outlets', description: 'Wall or floor power outlets', movable: false },
    { id: 8, name: 'Network Ports', description: 'Wired network connection ports', movable: false },
    { id: 9, name: 'Audio System', description: 'Classroom audio playback system', movable: false },
    { id: 10, name: 'Air Conditioning', description: 'Temperature control equipment', movable: false },
    { id: 11, name: 'Curtains', description: 'Blackout curtains for rooms requiring dark environment', movable: false },
    { id: 12, name: 'Lab Equipment', description: 'Specialized laboratory equipment', movable: false }
  ];

  // 教室设备数据，基于教室类型的默认配置
  export const mockClassroomEquipment = [
    // ComputerLab - 教室ID 1
    { id: 1, classroomId: 1, equipmentTypeId: 1, quantity: 1, status: 'Good' },
    { id: 2, classroomId: 1, equipmentTypeId: 2, quantity: 25, status: 'Good' },
    { id: 3, classroomId: 1, equipmentTypeId: 3, quantity: 1, status: 'Good' },
    { id: 4, classroomId: 1, equipmentTypeId: 4, quantity: 1, status: 'Good' },
    { id: 5, classroomId: 1, equipmentTypeId: 5, quantity: 25, status: 'Good' },
    { id: 6, classroomId: 1, equipmentTypeId: 7, quantity: 30, status: 'Good' },
    { id: 7, classroomId: 1, equipmentTypeId: 8, quantity: 26, status: 'Good' },
    { id: 8, classroomId: 1, equipmentTypeId: 9, quantity: 1, status: 'Good' },
    { id: 9, classroomId: 1, equipmentTypeId: 10, quantity: 2, status: 'Good' },
    { id: 10, classroomId: 1, equipmentTypeId: 11, quantity: 4, status: 'Needs Repair' },
    
    // ComputerLab - 教室ID 2
    { id: 11, classroomId: 2, equipmentTypeId: 1, quantity: 1, status: 'Good' },
    { id: 12, classroomId: 2, equipmentTypeId: 2, quantity: 30, status: 'Good' },
    { id: 13, classroomId: 2, equipmentTypeId: 3, quantity: 1, status: 'Good' },
    { id: 14, classroomId: 2, equipmentTypeId: 4, quantity: 1, status: 'Good' },
    { id: 15, classroomId: 2, equipmentTypeId: 5, quantity: 30, status: 'Partially Damaged' },
    { id: 16, classroomId: 2, equipmentTypeId: 7, quantity: 35, status: 'Good' },
    { id: 17, classroomId: 2, equipmentTypeId: 8, quantity: 31, status: 'Good' },
    { id: 18, classroomId: 2, equipmentTypeId: 9, quantity: 1, status: 'Needs Repair' },
    { id: 19, classroomId: 2, equipmentTypeId: 10, quantity: 2, status: 'Good' },
    { id: 20, classroomId: 2, equipmentTypeId: 11, quantity: 4, status: 'Good' },
    
    // Lecture - 教室ID 3
    { id: 21, classroomId: 3, equipmentTypeId: 1, quantity: 1, status: 'Good' },
    { id: 22, classroomId: 3, equipmentTypeId: 3, quantity: 1, status: 'Good' },
    { id: 23, classroomId: 3, equipmentTypeId: 4, quantity: 1, status: 'Good' },
    { id: 24, classroomId: 3, equipmentTypeId: 5, quantity: 80, status: 'Good' },
    { id: 25, classroomId: 3, equipmentTypeId: 7, quantity: 10, status: 'Good' },
    { id: 26, classroomId: 3, equipmentTypeId: 9, quantity: 1, status: 'Good' },
    { id: 27, classroomId: 3, equipmentTypeId: 10, quantity: 3, status: 'Good' },
    { id: 28, classroomId: 3, equipmentTypeId: 11, quantity: 6, status: 'Good' },
    
    // LargeHall - 教室ID 5
    { id: 29, classroomId: 5, equipmentTypeId: 1, quantity: 2, status: 'Good' },
    { id: 30, classroomId: 5, equipmentTypeId: 3, quantity: 1, status: 'Good' },
    { id: 31, classroomId: 5, equipmentTypeId: 4, quantity: 1, status: 'Good' },
    { id: 32, classroomId: 5, equipmentTypeId: 5, quantity: 150, status: 'Good' },
    { id: 33, classroomId: 5, equipmentTypeId: 7, quantity: 20, status: 'Good' },
    { id: 34, classroomId: 5, equipmentTypeId: 9, quantity: 2, status: 'Good' },
    { id: 35, classroomId: 5, equipmentTypeId: 10, quantity: 6, status: 'Good' },
    { id: 36, classroomId: 5, equipmentTypeId: 11, quantity: 10, status: 'Good' },
    
    // Laboratory - 教室ID 7
    { id: 37, classroomId: 7, equipmentTypeId: 1, quantity: 1, status: 'Good' },
    { id: 38, classroomId: 7, equipmentTypeId: 2, quantity: 10, status: 'Good' },
    { id: 39, classroomId: 7, equipmentTypeId: 4, quantity: 1, status: 'Good' },
    { id: 40, classroomId: 7, equipmentTypeId: 6, quantity: 15, status: 'Good' },
    { id: 41, classroomId: 7, equipmentTypeId: 7, quantity: 30, status: 'Good' },
    { id: 42, classroomId: 7, equipmentTypeId: 8, quantity: 15, status: 'Good' },
    { id: 43, classroomId: 7, equipmentTypeId: 9, quantity: 1, status: 'Good' },
    { id: 44, classroomId: 7, equipmentTypeId: 10, quantity: 2, status: 'Good' },
    { id: 45, classroomId: 7, equipmentTypeId: 12, quantity: 10, status: 'Partially Damaged' }
  ];

  // 设备与教室类型映射，描述每种教室类型默认应该有的设备
  export const mockRoomTypeEquipment = [
    { roomType: 'ComputerLab', equipmentTypeIds: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11] },
    { roomType: 'Lecture', equipmentTypeIds: [1, 3, 4, 5, 7, 9, 10, 11] },
    { roomType: 'LargeHall', equipmentTypeIds: [1, 3, 4, 5, 7, 9, 10, 11] },
    { roomType: 'Laboratory', equipmentTypeIds: [1, 2, 4, 6, 7, 8, 9, 10, 12] }
  ];
  
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

// 添加课程类型与教室类型匹配关系
export const mockCourseRoomTypeMatching = [
  { courseType: 'Computer Science', preferredRoomTypes: ['ComputerLab', 'Lecture'], requiredFeatures: ['Computers', 'Projector'] },
  { courseType: 'Mathematics', preferredRoomTypes: ['Lecture', 'LargeHall'], requiredFeatures: ['Whiteboard', 'Projector'] },
  { courseType: 'Physics', preferredRoomTypes: ['Laboratory', 'Lecture'], requiredFeatures: ['Lab Equipment'] },
  { courseType: 'Chemistry', preferredRoomTypes: ['Laboratory'], requiredFeatures: ['Lab Equipment', 'Safety Facilities'] },
  { courseType: 'Business', preferredRoomTypes: ['LargeHall', 'Lecture'], requiredFeatures: ['Projector', 'Audio System'] },
  { courseType: 'Language', preferredRoomTypes: ['Lecture'], requiredFeatures: ['Audio System'] },
  { courseType: 'Art', preferredRoomTypes: ['Lecture', 'LargeHall'], requiredFeatures: ['Projector'] }
];

// 添加课程与教室类型匹配系数
export const mockRoomTypeMatchScores = {
  'Computer Science': {
    'ComputerLab': 1.0,
    'Lecture': 0.7,
    'Laboratory': 0.5,
    'LargeHall': 0.3
  },
  'Mathematics': {
    'Lecture': 1.0,
    'LargeHall': 0.8,
    'ComputerLab': 0.5,
    'Laboratory': 0.3
  },
  'Physics': {
    'Laboratory': 1.0,
    'Lecture': 0.6,
    'ComputerLab': 0.5,
    'LargeHall': 0.4
  },
  'Chemistry': {
    'Laboratory': 1.0,
    'Lecture': 0.4,
    'ComputerLab': 0.3,
    'LargeHall': 0.2
  },
  'Business': {
    'LargeHall': 1.0,
    'Lecture': 0.8,
    'ComputerLab': 0.5,
    'Laboratory': 0.2
  },
  'Language': {
    'Lecture': 1.0,
    'LargeHall': 0.8,
    'ComputerLab': 0.6,
    'Laboratory': 0.3
  },
  'Art': {
    'Lecture': 1.0,
    'LargeHall': 0.9,
    'Laboratory': 0.5,
    'ComputerLab': 0.4
  }
};

// 添加课程与科目的关系，用于确定课程类型
export const mockCourseSubjectTypes = [
  { subjectId: 1, courseType: 'Computer Science' }, // Computer Science
  { subjectId: 2, courseType: 'Mathematics' },      // Mathematics
  { subjectId: 3, courseType: 'Physics' },          // Physics
  { subjectId: 4, courseType: 'Business' },         // Finance
  { subjectId: 5, courseType: 'Business' },         // Marketing
  { subjectId: 8, courseType: 'Economics' },        // Economics (假设subjectId=8是Economics)
  { subjectId: 9, courseType: 'Business' }          // Business (假设subjectId=9是Business)
];

// 添加findSuitableClassrooms函数实现
export const findSuitableClassrooms = (courseId, preferredRoomType = null, requiredFeatures = []) => {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      try {
        // 获取课程信息
        const course = mockCourses.find(c => c.id === courseId);
        if (!course) {
          reject(new Error('找不到指定的课程'));
          return;
        }
        
        // 获取课程类型
        const courseSubject = mockCourseSubjectTypes.find(cs => cs.subjectId === course.subjectId);
        const courseType = courseSubject?.courseType || '';
        
        // 获取该课程类型的推荐教室类型和设备
        const courseTypeMatching = mockCourseRoomTypeMatching.find(m => m.courseType === courseType);
        const preferredTypes = preferredRoomType ? 
          [preferredRoomType] : 
          (courseTypeMatching?.preferredRoomTypes || []);
        
        const requiredEquipment = requiredFeatures.length > 0 ? 
          requiredFeatures : 
          (courseTypeMatching?.requiredFeatures || []);
        
        // 筛选符合条件的教室
        let suitableRooms = [...mockClassrooms];
        
        // 检查容量是否满足课程需求
        suitableRooms = suitableRooms.filter(room => room.capacity >= course.enrollment);
        
        // 按教室类型筛选
        if (preferredTypes.length > 0 && !preferredTypes.includes('any')) {
          suitableRooms = suitableRooms.filter(room => preferredTypes.includes(room.type));
        }
        
        // 按必要设备筛选
        if (requiredEquipment.length > 0) {
          suitableRooms = suitableRooms.filter(room => {
            // 检查教室是否拥有所有必要设备
            return requiredEquipment.every(feature => 
              room.features && room.features.includes(feature)
            );
          });
        }
        
        // 计算匹配分数
        suitableRooms = suitableRooms.map(room => {
          // 基础分数
          let score = 0.5;
          
          // 如果教室类型匹配首选类型，增加分数
          if (preferredTypes.length > 0) {
            const typeIndex = preferredTypes.indexOf(room.type);
            if (typeIndex !== -1) {
              // 首选类型得分高，后面的依次降低
              score += 0.3 * (1 - (typeIndex * 0.2));
            }
          }
          
          // 计算设备匹配率
          if (requiredEquipment.length > 0) {
            const matchedFeatures = requiredEquipment.filter(f => 
              room.features && room.features.includes(f)
            ).length;
            
            const featureScore = matchedFeatures / requiredEquipment.length;
            score += featureScore * 0.2;
          }
          
          // 如果有充足的额外容量（不过多也不过少），增加分数
          const capacityRatio = room.capacity / course.enrollment;
          if (capacityRatio >= 1 && capacityRatio <= 1.5) {
            score += 0.1;
          } else if (capacityRatio > 1.5) {
            // 容量过大，适当减分（避免大教室被小课程占用）
            score -= Math.min(0.1, (capacityRatio - 1.5) * 0.05);
          }
          
          // 确保分数在0-1之间
          score = Math.max(0, Math.min(1, score));
          
          return {
            ...room,
            matchScore: score
          };
        });
        
        // 按匹配分数排序
        suitableRooms.sort((a, b) => b.matchScore - a.matchScore);
        
        resolve(suitableRooms);
      } catch (error) {
        reject(error);
      }
    }, 1000);
  });
};

// 添加assignClassroomsApi函数实现
export const assignClassroomsApi = (courseIds, constraints = {}) => {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      try {
        if (!courseIds || courseIds.length === 0) {
          reject(new Error('必须提供至少一个课程ID'));
          return;
        }
        
        const results = [];
        
        // 为每个课程分配教室
        for (const courseId of courseIds) {
          const course = mockCourses.find(c => c.id === courseId);
          if (!course) {
            results.push({
              courseId,
              roomId: null,
              matchScore: 0,
              roomName: '找不到课程信息',
              conflict: true
            });
            continue;
          }
          
          // 获取课程类型
          const subject = mockCourseSubjectTypes.find(cs => cs.subjectId === course.subjectId);
          const courseType = subject?.courseType || '';
          
          // 获取该课程类型的推荐教室类型和设备
          const matching = mockCourseRoomTypeMatching.find(m => m.courseType === courseType);
          
          // 查找合适的教室
          let preferredRoomTypes = matching?.preferredRoomTypes || [];
          const requiredFeatures = matching?.requiredFeatures || [];
          
          // 应用自定义约束
          if (constraints.preferredRoomType) {
            preferredRoomTypes = [constraints.preferredRoomType, ...preferredRoomTypes];
          }
          
          // 筛选符合条件的教室
          let suitableRooms = [...mockClassrooms];
          
          // 检查容量是否满足课程需求
          suitableRooms = suitableRooms.filter(room => room.capacity >= course.enrollment);
          
          // 按教室类型筛选
          if (preferredRoomTypes.length > 0 && !preferredRoomTypes.includes('any')) {
            suitableRooms = suitableRooms.filter(room => preferredRoomTypes.includes(room.type));
          }
          
          // 按必要设备筛选
          if (requiredFeatures.length > 0) {
            suitableRooms = suitableRooms.filter(room => {
              return requiredFeatures.every(feature => 
                room.features && room.features.includes(feature)
              );
            });
          }
          
          // 应用自定义约束中的设备要求
          if (constraints.requiredFeatures && constraints.requiredFeatures.length > 0) {
            suitableRooms = suitableRooms.filter(room => {
              return constraints.requiredFeatures.every(feature => 
                room.features && room.features.includes(feature)
              );
            });
          }
          
          // 计算匹配分数并排序
          suitableRooms = suitableRooms.map(room => {
            let score = 0.5;
            
            // 教室类型匹配
            if (preferredRoomTypes.length > 0) {
              const typeIndex = preferredRoomTypes.indexOf(room.type);
              if (typeIndex !== -1) {
                score += 0.3 * (1 - (typeIndex * 0.2));
              }
            }
            
            // 设备匹配
            const allRequiredFeatures = [...new Set([
              ...requiredFeatures,
              ...(constraints.requiredFeatures || [])
            ])];
            
            if (allRequiredFeatures.length > 0) {
              const matchedFeatures = allRequiredFeatures.filter(f => 
                room.features && room.features.includes(f)
              ).length;
              
              score += (matchedFeatures / allRequiredFeatures.length) * 0.2;
            }
            
            return {
              ...room,
              matchScore: Math.max(0, Math.min(1, score))
            };
          }).sort((a, b) => b.matchScore - a.matchScore);
          
          // 找到最佳匹配的教室
          const bestMatch = suitableRooms.length > 0 ? suitableRooms[0] : null;
          
          results.push({
            courseId,
            roomId: bestMatch?.id || null,
            matchScore: bestMatch?.matchScore || 0,
            roomName: bestMatch ? 
              `${bestMatch.building}-${bestMatch.name}` : 
              '未找到合适教室',
            features: bestMatch?.features || [],
            conflict: !bestMatch
          });
        }
        
        resolve(results);
      } catch (error) {
        reject(error);
      }
    }, 1500);
  });
};

