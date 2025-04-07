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
                SemesterId = 1,
                CourseSections = new List<CourseSectionInfo>(),
                Teachers = new List<TeacherInfo>(),
                Classrooms = new List<ClassroomInfo>(),
                TimeSlots = new List<TimeSlotInfo>(),
                TeacherCoursePreferences = new List<TeacherCoursePreference>(),
                TeacherAvailabilities = new List<TeacherAvailability>(),
                ClassroomAvailabilities = new List<ClassroomAvailability>(),
                Prerequisites = new List<CoursePrerequisite>()
            };
            // 生成时间槽
            GenerateTimeSlots(problem, timeSlotCount);
            // 生成教室
            GenerateClassrooms(problem, classroomCount);
            // 生成教师
            GenerateTeachers(problem, teacherCount);
            // 生成课程班级
            GenerateCourseSections(problem, courseSectionCount);
            // 生成教师课程偏好
            GenerateTeacherCoursePreferences(problem);
            // 生成教师可用性
            GenerateTeacherAvailabilities(problem);
            // 生成教室可用性
            GenerateClassroomAvailabilities(problem);
            // 生成先修课程关系
            GeneratePrerequisites(problem);
            // 生成约束(如果需要)
            GenerateConstraints(problem);
            

            return problem;
        }

        /// <summary>
        /// 生成测试时间槽
        /// </summary>
        private void GenerateTimeSlots(SchedulingProblem problem, int count)
            {
                // 每天5个时间段，周一到周五
                int slotsPerDay = Math.Min(5, (count + 4) / 5); // 向上取整
                int daysNeeded = Math.Min(5, (count + slotsPerDay - 1) / slotsPerDay); // 向上取整

                int slotId = 1;
                for (int day = 1; day <= daysNeeded && slotId <= count; day++)
                {
                    var slots = new List<(TimeSpan Start, TimeSpan End)>
                {
                    (new TimeSpan(8, 0, 0), new TimeSpan(9, 30, 0)),    // 8:00-9:30
                    (new TimeSpan(9, 40, 0), new TimeSpan(11, 10, 0)),  // 9:40-11:10
                    (new TimeSpan(13, 0, 0), new TimeSpan(14, 30, 0)),  // 13:00-14:30
                    (new TimeSpan(14, 40, 0), new TimeSpan(16, 10, 0)), // 14:40-16:10
                    (new TimeSpan(16, 20, 0), new TimeSpan(17, 50, 0)), // 16:20-17:50
                };

                for (int i = 0; i < slotsPerDay && slotId <= count; i++, slotId++)
                {
                    problem.TimeSlots.Add(new TimeSlotInfo
                    {
                        Id = slotId,
                        DayOfWeek = day,
                        DayName = GetDayName(day),
                        StartTime = slots[i].Start,
                        EndTime = slots[i].End
                    });
                }
            }
        }

        /// <summary>
        /// 生成测试教室
        /// </summary>
        private void GenerateClassrooms(SchedulingProblem problem, int count)
        {
            string[] buildings = { "A", "B", "Science", "Library" };
            string[] types = { "Regular", "Computer", "Lab", "Lecture" };

            for (int i = 1; i <= count; i++)
            {
                string building = buildings[_random.Next(buildings.Length)];
                string type = types[_random.Next(types.Length)];

                // 根据教室类型决定容量
                int capacity = type switch
                {
                    "Regular" => _random.Next(30, 51),
                    "Computer" => _random.Next(20, 31),
                    "Lab" => _random.Next(15, 26),
                    "Lecture" => _random.Next(60, 121),
                    _ => _random.Next(30, 51)
                };

                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"{building}-{100 + i}",
                    Building = building,
                    CampusId = _random.Next(1, 3), // 1或2，表示不同校区
                    CampusName = _random.Next(1, 3) == 1 ? "Main Campus" : "East Campus",
                    Capacity = capacity,
                    Type = type,
                    HasComputers = type == "Computer",
                    HasProjector = _random.NextDouble() > 0.2 // 80%的概率有投影仪
                });
            }
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

        private void GenerateTeachers(SchedulingProblem problem, int count)
        {
            string[] firstNames = { "John", "Mary", "David", "Sarah", "Michael", "Linda", "Robert", "Patricia" };
            string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Garcia" };
            string[] titles = { "Professor", "Associate Professor", "Assistant Professor", "Lecturer" };
            string[] departments = { "Computer Science", "Mathematics", "Physics", "Engineering" };

            for (int i = 1; i <= count; i++)
            {
                string firstName = firstNames[_random.Next(firstNames.Length)];
                string lastName = lastNames[_random.Next(lastNames.Length)];
                string title = titles[_random.Next(titles.Length)];
                int departmentId = _random.Next(1, departments.Length + 1);

                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"{firstName} {lastName}",
                    Title = title,
                    DepartmentId = departmentId,
                    DepartmentName = departments[departmentId - 1],
                    MaxWeeklyHours = _random.Next(12, 21),
                    MaxDailyHours = _random.Next(4, 7),
                    MaxConsecutiveHours = _random.Next(2, 4)
                });
            }
        }

        private void GenerateCourseSections(SchedulingProblem problem, int count)
        {
            string[] subjects = { "CS", "MATH", "PHYS", "ENG" };
            string[] courseNames = {
                "Introduction to Programming",
                "Data Structures",
                "Algorithms",
                "Calculus I",
                "Linear Algebra",
                "Mechanics",
                "Electricity and Magnetism",
                "Engineering Principles"
            };      

            // 生成课程，确保课程数不大于课程名称数量
            int numCourses = Math.Min(courseNames.Length, (count + 1) / 2); // 平均每门课有2个班级
            var courses = new List<(int Id, string Code, string Name, int Credits, string Type)>();

            for (int i = 1; i <= numCourses; i++)
            {
                string subject = subjects[_random.Next(subjects.Length)];
                string name = courseNames[i - 1];
                int credits = _random.Next(2, 5);
                string type = _random.NextDouble() > 0.7 ? (_random.NextDouble() > 0.5 ? "Lab" : "Computer") : "Regular";

                courses.Add((i, $"{subject}{100 + i}", name, credits, type));
            }

            // 生成班级
            for (int i = 1; i <= count; i++)
            {
                var course = courses[_random.Next(courses.Count)];
                int enrollment = _random.Next(15, 41);

                problem.CourseSections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = course.Id,
                    CourseCode = course.Code,
                    CourseName = course.Name,
                    SectionCode = $"{course.Code}-{(char)('A' + _random.Next(3))}",
                    Credits = course.Credits,
                    Hours = course.Credits * 2,
                    CourseType = course.Type,
                    DepartmentId = _random.Next(1, 5),
                    DepartmentName = "Department " + _random.Next(1, 5),
                    Enrollment = enrollment,
                    MaxEnrollment = enrollment + _random.Next(5, 11),
                    RequiredRoomType = course.Type
                });
            }
        }

        private void GenerateTeacherCoursePreferences(SchedulingProblem problem)
        {
            // 为每个教师生成一些课程偏好
            foreach (var teacher in problem.Teachers)
            {
                // 随机选择2-4门课程
                int numCourses = _random.Next(2, Math.Min(5, problem.CourseSections.Count + 1));
                var courseIds = problem.CourseSections
                    .Select(s => s.CourseId)
                    .Distinct()
                    .OrderBy(x => _random.Next())
                    .Take(numCourses)
                    .ToList();

                foreach (var courseId in courseIds)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = courseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5
                        PreferenceLevel = _random.Next(2, 6)   // 2-5
                    });
                }
            }
        }

        private void GenerateTeacherAvailabilities(SchedulingProblem problem)
        {
            // 默认教师在所有时间段都可用，只需要生成不可用时间段
            foreach (var teacher in problem.Teachers)
            {
                // 随机选择0-3个不可用时间段
                int unavailableCount = _random.Next(4);
                var unavailableSlots = problem.TimeSlots
                    .OrderBy(x => _random.Next())
                    .Take(unavailableCount)
                    .ToList();

                foreach (var slot in unavailableSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = false,
                        PreferenceLevel = 0
                    });
                }

                // 随机选择1-2个高偏好时间段
                int preferredCount = _random.Next(1, 3);
                var preferredSlots = problem.TimeSlots
                    .Except(unavailableSlots)
                    .OrderBy(x => _random.Next())
                    .Take(preferredCount)
                    .ToList();

                foreach (var slot in preferredSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = true,
                        PreferenceLevel = 5 // 最高偏好
                    });
                }
            }
        }

        private void GenerateClassroomAvailabilities(SchedulingProblem problem)
        {
            // 默认教室在所有时间段都可用，只需要生成不可用时间段
            foreach (var classroom in problem.Classrooms)
            {
                // 随机选择0-2个不可用时间段
                int unavailableCount = _random.Next(3);
                var unavailableSlots = problem.TimeSlots
                    .OrderBy(x => _random.Next())
                    .Take(unavailableCount)
                    .ToList();

                foreach (var slot in unavailableSlots)
                {
                    problem.ClassroomAvailabilities.Add(new ClassroomAvailability
                    {
                        ClassroomId = classroom.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = false,
                        UnavailableReason = "Maintenance"
                    });
                }
            }
        }

        private void GeneratePrerequisites(SchedulingProblem problem)
        {
            // 获取所有不同的课程ID
            var courseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            // 只有多于一门课程时才能设置先修关系
            if (courseIds.Count > 1)
            {
                // 随机生成1-3个先修关系
                int prereqCount = Math.Min(_random.Next(1, 4), courseIds.Count - 1);

                for (int i = 0; i < prereqCount; i++)
                {
                    // 随机选择一门后续课程
                    int laterCourseIndex = _random.Next(1, courseIds.Count);
                    int laterCourseId = courseIds[laterCourseIndex];

                    // 选择一门先修课程(必须是ID较小的课程)
                    int prereqCourseIndex = _random.Next(laterCourseIndex);
                    int prereqCourseId = courseIds[prereqCourseIndex];

                    // 添加先修关系
                    problem.Prerequisites.Add(new CoursePrerequisite
                    {
                        CourseId = laterCourseId,
                        PrerequisiteCourseId = prereqCourseId
                    });
                }
            }
        }

        private void GenerateConstraints(SchedulingProblem problem)
        {
            // 在实际使用时，约束是通过依赖注入提供的，这里只是示例
            // 实现位于DependencyInjection.cs中注册
        }

        private string GetDayName(int day)
        {
            return day switch
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

        // 用于生成测试解决方案
        public SchedulingSolution CreateTestSolution(SchedulingProblem problem = null)
        {
            problem ??= GenerateTestProblem();

            var solution = new SchedulingSolution
            {
                Id = 1,
                ProblemId = problem.Id,
                Problem = problem,
                Name = "Test Solution",
                CreatedAt = DateTime.Now,
                Algorithm = "Test",
                Assignments = new List<SchedulingAssignment>()
            };

            // 为每个课程班级创建随机分配
            int assignmentId = 1;
            foreach (var section in problem.CourseSections)
            {
                // 随机选择时间槽、教室和教师
                var timeSlot = problem.TimeSlots.OrderBy(x => _random.Next()).FirstOrDefault();
                var classroom = problem.Classrooms.OrderBy(x => _random.Next()).FirstOrDefault();

                // 选择有资格教授此课程的教师
                var qualifiedTeachers = problem.TeacherCoursePreferences
                    .Where(p => p.CourseId == section.CourseId)
                    .Select(p => p.TeacherId)
                    .ToList();

                var teacher = qualifiedTeachers.Count > 0
                    ? problem.Teachers.FirstOrDefault(t => qualifiedTeachers.Contains(t.Id))
                    : problem.Teachers.OrderBy(x => _random.Next()).FirstOrDefault();

                if (timeSlot != null && classroom != null && teacher != null)
                {
                    solution.Assignments.Add(new SchedulingAssignment
                    {
                        Id = assignmentId++,
                        SectionId = section.Id,
                        SectionCode = section.SectionCode,
                        ClassroomId = classroom.Id,
                        ClassroomName = classroom.Name,
                        TeacherId = teacher.Id,
                        TeacherName = teacher.Name,
                        TimeSlotId = timeSlot.Id,
                        DayOfWeek = timeSlot.DayOfWeek,
                        StartTime = timeSlot.StartTime,
                        EndTime = timeSlot.EndTime,
                        WeekPattern = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
                    });
                }
            }

            return solution;
        }
    }
}