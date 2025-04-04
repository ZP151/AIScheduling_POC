using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Utils
{
    /// <summary>
    /// 用于生成测试数据的工具类
    /// </summary>
    public class TestDataGenerator
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// 生成测试排课问题
        /// </summary>
        /// <param name="courseSectionCount">课程班级数量</param>
        /// <param name="teacherCount">教师数量</param>
        /// <param name="classroomCount">教室数量</param>
        /// <param name="timeSlotCount">时间槽数量</param>
        /// <returns>测试排课问题</returns>
        public SchedulingProblem GenerateTestProblem(
            int courseSectionCount = 20,
            int teacherCount = 10,
            int classroomCount = 15,
            int timeSlotCount = 30)
        {
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = $"Test Problem {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                SemesterId = 1
            };

            // 生成时间槽
            problem.TimeSlots = GenerateTimeSlots(timeSlotCount);

            // 生成教室
            problem.Classrooms = GenerateClassrooms(classroomCount);

            // 生成教师
            problem.Teachers = GenerateTeachers(teacherCount);

            // 生成课程班级
            problem.CourseSections = GenerateCourseSections(courseSectionCount);

            // 生成教师课程偏好
            problem.TeacherCoursePreferences = GenerateTeacherCoursePreferences(problem);

            // 生成教师可用性
            problem.TeacherAvailabilities = GenerateTeacherAvailabilities(problem);

            // 生成教室可用性
            problem.ClassroomAvailabilities = GenerateClassroomAvailabilities(problem);

            return problem;
        }

        /// <summary>
        /// 生成测试时间槽
        /// </summary>
        private List<TimeSlotInfo> GenerateTimeSlots(int count)
        {
            var timeSlots = new List<TimeSlotInfo>();

            // 每天6个时间段，周一到周五
            for (int day = 1; day <= 5; day++)
            {
                var daySlots = new List<(TimeSpan Start, TimeSpan End)>
                {
                    (new TimeSpan(8, 0, 0), new TimeSpan(9, 30, 0)),    // 8:00-9:30
                    (new TimeSpan(9, 40, 0), new TimeSpan(11, 10, 0)),  // 9:40-11:10
                    (new TimeSpan(11, 20, 0), new TimeSpan(12, 50, 0)), // 11:20-12:50
                    (new TimeSpan(14, 0, 0), new TimeSpan(15, 30, 0)),  // 14:00-15:30
                    (new TimeSpan(15, 40, 0), new TimeSpan(17, 10, 0)), // 15:40-17:10
                    (new TimeSpan(18, 30, 0), new TimeSpan(20, 0, 0)),  // 18:30-20:00
                };

                foreach (var (start, end) in daySlots)
                {
                    if (timeSlots.Count >= count) break;

                    timeSlots.Add(new TimeSlotInfo
                    {
                        Id = timeSlots.Count + 1,
                        DayOfWeek = day,
                        DayName = GetDayName(day),
                        StartTime = start,
                        EndTime = end
                    });
                }

                if (timeSlots.Count >= count) break;
            }

            return timeSlots;
        }

        /// <summary>
        /// 生成测试教室
        /// </summary>
        private List<ClassroomInfo> GenerateClassrooms(int count)
        {
            var classrooms = new List<ClassroomInfo>();
            var buildings = new[] { "A", "B", "C", "Science", "Library" };
            var roomTypes = new[] { "Regular", "Computer", "Lab", "Lecture", "Seminar" };

            for (int i = 1; i <= count; i++)
            {
                string building = buildings[_random.Next(buildings.Length)];
                string roomType = roomTypes[_random.Next(roomTypes.Length)];
                int capacity = roomType switch
                {
                    "Regular" => _random.Next(30, 61),
                    "Computer" => _random.Next(20, 41),
                    "Lab" => _random.Next(15, 31),
                    "Lecture" => _random.Next(80, 201),
                    "Seminar" => _random.Next(10, 26),
                    _ => _random.Next(20, 41)
                };

                // 生成教室编号
                string roomNumber = $"{_random.Next(100, 500)}";

                classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"{building}-{roomNumber}",
                    Building = building,
                    CampusId = _random.Next(1, 3), // 1或2，表示不同校区
                    CampusName = _random.Next(1, 3) == 1 ? "Main Campus" : "East Campus",
                    Capacity = capacity,
                    Type = roomType,
                    Equipment = GenerateEquipment(roomType),
                    HasComputers = roomType == "Computer",
                    HasProjector = _random.NextDouble() > 0.2 // 80%的教室有投影仪
                });
            }

            return classrooms;
        }

        /// <summary>
        /// 生成教室设备列表
        /// </summary>
        private string GenerateEquipment(string roomType)
        {
            var equipment = new List<string>();

            // 基本设备
            if (_random.NextDouble() > 0.1) equipment.Add("Whiteboard");
            if (_random.NextDouble() > 0.2) equipment.Add("Projector");

            // 根据教室类型添加特殊设备
            switch (roomType)
            {
                case "Computer":
                    equipment.Add("Computers");
                    if (_random.NextDouble() > 0.5) equipment.Add("Printer");
                    break;
                case "Lab":
                    equipment.Add("LabBench");
                    if (_random.NextDouble() > 0.5) equipment.Add("SafetyEquipment");
                    break;
                case "Lecture":
                    equipment.Add("Microphone");
                    if (_random.NextDouble() > 0.3) equipment.Add("SoundSystem");
                    break;
                case "Seminar":
                    if (_random.NextDouble() > 0.5) equipment.Add("RoundTable");
                    break;
            }

            return string.Join(",", equipment);
        }

        /// <summary>
        /// 生成测试教师
        /// </summary>
        private List<TeacherInfo> GenerateTeachers(int count)
        {
            var teachers = new List<TeacherInfo>();
            var firstNames = new[] { "John", "Mary", "David", "Sarah", "Michael", "Linda", "Robert", "Patricia", "James", "Jennifer" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Garcia", "Rodriguez", "Wilson" };
            var titles = new[] { "Professor", "Associate Professor", "Assistant Professor", "Lecturer", "Instructor" };
            var departments = new[] { "Computer Science", "Mathematics", "Physics", "Chemistry", "Biology", "Engineering" };

            for (int i = 1; i <= count; i++)
            {
                string firstName = firstNames[_random.Next(firstNames.Length)];
                string lastName = lastNames[_random.Next(lastNames.Length)];
                string title = titles[_random.Next(titles.Length)];
                int departmentId = _random.Next(1, departments.Length + 1);

                teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"{firstName} {lastName}",
                    Title = title,
                    DepartmentId = departmentId,
                    DepartmentName = departments[departmentId - 1],
                    MaxWeeklyHours = _random.Next(12, 25),
                    MaxDailyHours = _random.Next(4, 9),
                    MaxConsecutiveHours = _random.Next(2, 5),
                    PreferredBuilding = _random.NextDouble() > 0.5 ? null : new[] { "A", "B", "C", "Science", "Library" }[_random.Next(5)]
                });
            }

            return teachers;
        }

        /// <summary>
        /// 生成测试课程班级
        /// </summary>
        private List<CourseSectionInfo> GenerateCourseSections(int count)
        {
            var sections = new List<CourseSectionInfo>();
            var courseNames = new[]
            {
                "Introduction to Programming",
                "Data Structures",
                "Algorithms",
                "Database Systems",
                "Calculus I",
                "Linear Algebra",
                "Physics I",
                "Chemistry I",
                "Biology I",
                "Engineering Principles"
            };
            var courseCodes = new[] { "CS101", "CS201", "CS301", "CS401", "MATH101", "MATH201", "PHYS101", "CHEM101", "BIO101", "ENG101" };
            var sectionCodes = new[] { "A", "B", "C", "D", "E" };
            var courseTypes = new[] { "Lecture", "Lab", "Computer", "Seminar" };
            var departments = new[] { "Computer Science", "Mathematics", "Physics", "Chemistry", "Biology", "Engineering" };

            for (int i = 1; i <= count; i++)
            {
                int courseIndex = _random.Next(courseNames.Length);
                string courseType = courseTypes[_random.Next(courseTypes.Length)];
                int departmentId = _random.Next(1, departments.Length + 1);
                int credits = _random.Next(2, 6);
                int enrollment = _random.Next(10, 101);

                sections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = (courseIndex + 1),
                    CourseCode = courseCodes[courseIndex],
                    CourseName = courseNames[courseIndex],
                    SectionCode = $"{courseCodes[courseIndex]}-{sectionCodes[_random.Next(sectionCodes.Length)]}",
                    Credits = credits,
                    Hours = credits * 2,
                    CourseType = courseType,
                    DepartmentId = departmentId,
                    DepartmentName = departments[departmentId - 1],
                    Enrollment = enrollment,
                    MaxEnrollment = enrollment + _random.Next(5, 21),
                    RequiredRoomType = courseType,
                    RequiredEquipment = courseType == "Computer" ? "Computers" :
                                        courseType == "Lab" ? "LabBench" :
                                        _random.NextDouble() > 0.5 ? "Projector" : null,
                    RequiresSameTeacher = _random.NextDouble() > 0.7,
                    RequiresSameRoom = _random.NextDouble() > 0.8
                });
            }

            return sections;
        }

        /// <summary>
        /// 生成教师课程偏好
        /// </summary>
        private List<TeacherCoursePreference> GenerateTeacherCoursePreferences(SchedulingProblem problem)
        {
            var preferences = new List<TeacherCoursePreference>();

            // 为每个教师随机分配课程偏好
            foreach (var teacher in problem.Teachers)
            {
                // 在同一个部门内的课程更有可能被分配给该教师
                var departmentCourses = problem.CourseSections
                    .Where(cs => cs.DepartmentId == teacher.DepartmentId)
                    .ToList();

                // 随机选择2-5门课程
                int courseCount = _random.Next(2, Math.Min(6, departmentCourses.Count + 1));

                for (int i = 0; i < courseCount; i++)
                {
                    if (departmentCourses.Count == 0) break;

                    int randomIndex = _random.Next(departmentCourses.Count);
                    var course = departmentCourses[randomIndex];
                    departmentCourses.RemoveAt(randomIndex);

                    preferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = course.CourseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5
                        PreferenceLevel = _random.Next(3, 6)   // 3-5
                    });
                }

                // 可能还会教授少量其他部门的课程
                if (_random.NextDouble() > 0.7)
                {
                    var otherCourses = problem.CourseSections
                        .Where(cs => cs.DepartmentId != teacher.DepartmentId)
                        .ToList();

                    if (otherCourses.Count > 0)
                    {
                        int otherCourseCount = _random.Next(1, 3);
                        for (int i = 0; i < otherCourseCount; i++)
                        {
                            int randomIndex = _random.Next(otherCourses.Count);
                            var course = otherCourses[randomIndex];
                            otherCourses.RemoveAt(randomIndex);

                            preferences.Add(new TeacherCoursePreference
                            {
                                TeacherId = teacher.Id,
                                CourseId = course.CourseId,
                                ProficiencyLevel = _random.Next(2, 5), // 2-4
                                PreferenceLevel = _random.Next(1, 4)   // 1-3
                            });
                        }
                    }
                }
            }

            return preferences;
        }

        /// <summary>
        /// 生成教师可用性
        /// </summary>
        private List<TeacherAvailability> GenerateTeacherAvailabilities(SchedulingProblem problem)
        {
            var availabilities = new List<TeacherAvailability>();

            // 假设每位教师有少量不可用时间段
            foreach (var teacher in problem.Teachers)
            {
                // 大部分时间段默认可用，不需要特别添加

                // 添加少量不可用时间段
                int unavailableCount = _random.Next(1, 6); // 1-5个不可用时间段

                var unavailableSlotIndices = new HashSet<int>();
                while (unavailableSlotIndices.Count < unavailableCount && unavailableSlotIndices.Count < problem.TimeSlots.Count)
                {
                    unavailableSlotIndices.Add(_random.Next(problem.TimeSlots.Count));
                }

                foreach (int index in unavailableSlotIndices)
                {
                    availabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = problem.TimeSlots[index].Id,
                        IsAvailable = false,
                        PreferenceLevel = 1, // 不喜欢这个时间段
                        ApplicableWeeks = GenerateWeeks(1, 15)
                    });
                }

                // 添加少量特别偏好的时间段
                int preferredCount = _random.Next(1, 4); // 1-3个特别偏好的时间段

                var preferredSlotIndices = new HashSet<int>();
                while (preferredSlotIndices.Count < preferredCount && preferredSlotIndices.Count < problem.TimeSlots.Count)
                {
                    int index = _random.Next(problem.TimeSlots.Count);
                    if (!unavailableSlotIndices.Contains(index))
                    {
                        preferredSlotIndices.Add(index);
                    }
                }

                foreach (int index in preferredSlotIndices)
                {
                    availabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = problem.TimeSlots[index].Id,
                        IsAvailable = true,
                        PreferenceLevel = 5, // 非常喜欢这个时间段
                        ApplicableWeeks = GenerateWeeks(1, 15)
                    });
                }
            }

            return availabilities;
        }

        /// <summary>
        /// 生成教室可用性
        /// </summary>
        private List<ClassroomAvailability> GenerateClassroomAvailabilities(SchedulingProblem problem)
        {
            var availabilities = new List<ClassroomAvailability>();

            // 假设每个教室有少量不可用时间段
            foreach (var classroom in problem.Classrooms)
            {
                // 大部分时间段默认可用，不需要特别添加

                // 添加少量不可用时间段
                int unavailableCount = _random.Next(0, 4); // 0-3个不可用时间段

                var unavailableSlotIndices = new HashSet<int>();
                while (unavailableSlotIndices.Count < unavailableCount && unavailableSlotIndices.Count < problem.TimeSlots.Count)
                {
                    unavailableSlotIndices.Add(_random.Next(problem.TimeSlots.Count));
                }

                foreach (int index in unavailableSlotIndices)
                {
                    availabilities.Add(new ClassroomAvailability
                    {
                        ClassroomId = classroom.Id,
                        TimeSlotId = problem.TimeSlots[index].Id,
                        IsAvailable = false,
                        UnavailableReason = "Maintenance",
                        ApplicableWeeks = GenerateWeeks(1, 15)
                    });
                }
            }

            return availabilities;
        }

        /// <summary>
        /// 生成周次列表
        /// </summary>
        private List<int> GenerateWeeks(int start, int end)
        {
            var weeks = new List<int>();
            for (int i = start; i <= end; i++)
            {
                weeks.Add(i);
            }
            return weeks;
        }

        /// <summary>
        /// 获取星期几的名称
        /// </summary>
        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                7 => "Sunday",
                _ => "Unknown"
            };
        }
    }
}