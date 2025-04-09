using SmartSchedulingSystem.Scheduling.Constraints;
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

            // 1. 生成时间槽 - 确保时间槽足够多
            GenerateTimeSlots(problem, Math.Max(courseSectionCount, timeSlotCount));

            // 2. 生成教室 - 确保容量足够
            GenerateClassrooms(problem, classroomCount);

            // 3. 生成教师
            GenerateTeachers(problem, teacherCount);

            // 4. 生成课程班级 - 确保班级大小合理
            GenerateCourseSections(problem, courseSectionCount);

            // 5. 确保每门课程至少有一位合格的教师
            EnsureQualifiedTeachersForAllCourses(problem);

            // 6. 限制不可用时间段的数量
            LimitUnavailabilityPeriods(problem);

            // 7. 谨慎处理先修课程关系
            GenerateSafePrerequisites(problem);

            return problem;
        }

        /// <summary>
        /// 生成测试时间槽
        /// </summary>
        // 生成合理的时间槽
        private void GenerateTimeSlots(SchedulingProblem problem, int count)
        {
            // 确保有足够的时间槽
            int slotsPerDay = Math.Min(6, (count + 4) / 5); // 每天最多6个时间段
            int daysNeeded = Math.Min(5, (count + slotsPerDay - 1) / slotsPerDay); // 最多5天

            // 确保有足够的时间槽容纳所有课程
            int totalSlots = daysNeeded * slotsPerDay;
            if (totalSlots < count)
            {
                // 如果不够，增加每天的时间段
                slotsPerDay = (int)Math.Ceiling((double)count / daysNeeded);
            }

            int slotId = 1;
            for (int day = 1; day <= daysNeeded; day++)
            {
                for (int slot = 0; slot < slotsPerDay && slotId <= count; slot++, slotId++)
                {
                    // 创建均匀分布的时间段
                    var startHour = 8 + slot * 2; // 从8点开始，每段2小时
                    var endHour = startHour + 1;  // 每节课1小时

                    problem.TimeSlots.Add(new TimeSlotInfo
                    {
                        Id = slotId,
                        DayOfWeek = day,
                        DayName = GetDayName(day),
                        StartTime = new TimeSpan(startHour, 0, 0),
                        EndTime = new TimeSpan(endHour, 30, 0)
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

        // 生成课程班级，确保班级大小合理
        private void GenerateCourseSections(SchedulingProblem problem, int count)
        {
            string[] subjects = { "CS", "MATH", "PHYS", "ENG" };
            string[] courseNames = {
        "Introduction to Programming", "Data Structures", "Algorithms",
        "Calculus I", "Linear Algebra", "Mechanics",
        "Electricity and Magnetism", "Engineering Principles", "Database Systems",
        "Computer Networks", "Operating Systems", "Artificial Intelligence",
        "Software Engineering", "Web Development", "Mobile Computing",
        "Computer Graphics", "Human-Computer Interaction", "Cryptography",
        "Machine Learning", "Robotics"
    };

            // 确保有足够的课程名称
            if (count > courseNames.Length)
            {
                // 扩展课程名称列表
                var extraNames = Enumerable.Range(1, count - courseNames.Length)
                    .Select(i => $"Course {courseNames.Length + i}")
                    .ToList();
                courseNames = courseNames.Concat(extraNames).ToArray();
            }

            // 生成课程，确保每门课程最多有少量班级
            int numCourses = Math.Max(count / 2, 1); // 至少一门课程
            var courses = new List<(int Id, string Code, string Name, int Credits, string Type)>();

            for (int i = 1; i <= numCourses; i++)
            {
                string subject = subjects[_random.Next(subjects.Length)];
                string name = courseNames[i - 1];
                int credits = _random.Next(2, 5);
                string type = _random.NextDouble() > 0.7 ? (_random.NextDouble() > 0.5 ? "Lab" : "Computer") : "Regular";

                courses.Add((i, $"{subject}{100 + i}", name, credits, type));
            }

            // 生成班级 - 限制学生人数不超过30人，确保能放进任何教室
            for (int i = 1; i <= count; i++)
            {
                var course = courses[_random.Next(courses.Count)];
                int enrollment = _random.Next(15, 31); // 最大30人

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
        // 确保每门课程至少有一位合格的教师
        private void EnsureQualifiedTeachersForAllCourses(SchedulingProblem problem)
        {
            // 清除任何可能存在的教师课程偏好
            problem.TeacherCoursePreferences.Clear();

            // 获取所有不同的课程ID
            var distinctCourseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .ToList();

            // 对每门课程，确保至少有两位教师有资格教授
            foreach (var courseId in distinctCourseIds)
            {
                // 选择至少两位教师（或全部如果教师总数少于2）
                int minTeachers = Math.Min(2, problem.Teachers.Count);
                int teacherCount = _random.Next(minTeachers, problem.Teachers.Count + 1);
                var selectedTeachers = problem.Teachers
                    .OrderBy(x => _random.Next())
                    .Take(teacherCount)
                    .ToList();

                foreach (var teacher in selectedTeachers)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = courseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5，确保有资格
                        PreferenceLevel = _random.Next(3, 6)   // 3-5，确保有较高偏好
                    });
                }
            }

            // 额外添加一些随机偏好，增加测试数据的多样性
            foreach (var teacher in problem.Teachers)
            {
                foreach (var courseId in distinctCourseIds)
                {
                    // 如果已经添加过此组合，跳过
                    if (problem.TeacherCoursePreferences.Any(p =>
                        p.TeacherId == teacher.Id && p.CourseId == courseId))
                    {
                        continue;
                    }

                    // 随机添加一些额外的偏好
                    if (_random.NextDouble() < 0.3) // 30%的概率添加
                    {
                        problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                        {
                            TeacherId = teacher.Id,
                            CourseId = courseId,
                            ProficiencyLevel = _random.Next(1, 6), // 1-5，包括不太合格的
                            PreferenceLevel = _random.Next(1, 6)   // 1-5，包括低偏好的
                        });
                    }
                }
            }
        }
        // 限制不可用时间段数量，确保每个资源至少有足够的可用时间
        private void LimitUnavailabilityPeriods(SchedulingProblem problem)
        {
            // 清除现有的可用性设置
            problem.TeacherAvailabilities.Clear();
            problem.ClassroomAvailabilities.Clear();

            // 计算每个资源最多可以有多少不可用时段
            int maxUnavailablePerTeacher = Math.Max(1, problem.TimeSlots.Count / 10);
            int maxUnavailablePerClassroom = Math.Max(1, problem.TimeSlots.Count / 20);

            // 为教师添加少量不可用时间段
            foreach (var teacher in problem.Teachers)
            {
                // 随机选择少量不可用时间段
                int unavailableCount = _random.Next(maxUnavailablePerTeacher + 1);
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

                // 随机添加一些高偏好时间段
                int preferredCount = _random.Next(1, 4); // 1-3个高偏好时段
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

            // 为教室添加少量不可用时间段
            foreach (var classroom in problem.Classrooms)
            {
                // 随机选择少量不可用时间段
                int unavailableCount = _random.Next(maxUnavailablePerClassroom + 1);
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

        // 生成安全的先修课程关系（避免同一学期内的相互依赖）
        private void GenerateSafePrerequisites(SchedulingProblem problem)
        {
            // 为简单起见，在测试数据中不添加先修课程约束
            problem.Prerequisites.Clear();

            // 如果需要添加一些简单的先修关系，可以采用以下方法
            // 但要确保这些关系不会导致循环依赖
            /*
            var courseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            if (courseIds.Count >= 3)
            {
                // 只为第三门及以后的课程添加先修关系
                // 并确保先修课程的ID总是小于当前课程
                for (int i = 2; i < Math.Min(5, courseIds.Count); i++)
                {
                    int courseId = courseIds[i];
                    int prereqId = courseIds[i - 2]; // 确保有足够距离，避免冲突

                    problem.Prerequisites.Add(new CoursePrerequisite
                    {
                        CourseId = courseId,
                        PrerequisiteCourseId = prereqId
                    });
                }
            }
            */
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

        public SchedulingProblem CreateSimpleValidProblem()
        {
            // 创建基本问题结构
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Simple Valid Test Problem",
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

            // 创建时间槽 (3天×2时段=6个时间槽)
            for (int day = 1; day <= 3; day++)
            {
                for (int slot = 0; slot < 2; slot++)
                {
                    var startTime = new TimeSpan(8 + 2 * slot, 0, 0);
                    var endTime = new TimeSpan(9 + 2 * slot, 30, 0);

                    problem.TimeSlots.Add(new TimeSlotInfo
                    {
                        Id = (day - 1) * 2 + slot + 1,
                        DayOfWeek = day,
                        DayName = $"Day {day}",
                        StartTime = startTime,
                        EndTime = endTime
                    });
                }
            }

            // 创建教室 (2个教室)
            for (int i = 1; i <= 2; i++)
            {
                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"Room {i}",
                    Building = "Main Building",
                    CampusId = 1,
                    CampusName = "Main Campus",
                    Capacity = 30 + (i - 1) * 10, // 第一个教室容量30人，第二个教室容量40人
                    Type = "Regular",
                    HasComputers = false,
                    HasProjector = true
                });
            }

            // 创建教师 (2位教师)
            for (int i = 1; i <= 2; i++)
            {
                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"Teacher {i}",
                    Title = "Professor",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    MaxWeeklyHours = 20,
                    MaxDailyHours = 6,
                    MaxConsecutiveHours = 3
                });
            }

            // 创建课程班级 (3个课程班级)
            int[] enrollments = { 25, 35, 20 };
            for (int i = 1; i <= 3; i++)
            {
                problem.CourseSections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = i,
                    CourseCode = $"CS10{i}",
                    CourseName = $"Course {i}",
                    SectionCode = $"CS10{i}-A",
                    Credits = 3,
                    Hours = 6,
                    Enrollment = enrollments[i - 1],
                    MaxEnrollment = enrollments[i - 1] + 5,
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    CourseType = "Regular",
                    RequiredRoomType = "Regular"
                });
            }

            // 添加教师课程偏好 (确保每位教师都能教授所有课程)
            foreach (var teacher in problem.Teachers)
            {
                foreach (var course in problem.CourseSections)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = course.CourseId,
                        ProficiencyLevel = 5, // 高能力值
                        PreferenceLevel = 4   // 高偏好
                    });
                }
            }

            // 确保所有教师在所有时间都可用
            foreach (var teacher in problem.Teachers)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 不需要显式添加可用记录，默认都视为可用
                }
            }

            // 确保所有教室在所有时间都可用
            foreach (var classroom in problem.Classrooms)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 不需要显式添加可用记录，默认都视为可用
                }
            }

            return problem;
        }
        // 添加到TestDataGenerator类中
        public SchedulingProblem CreateGuaranteedFeasibleProblem()
        {
            // 创建基本问题结构
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Guaranteed Feasible Problem",
                SemesterId = 1,
                CourseSections = new List<CourseSectionInfo>(),
                Teachers = new List<TeacherInfo>(),
                Classrooms = new List<ClassroomInfo>(),
                TimeSlots = new List<TimeSlotInfo>(),
                TeacherCoursePreferences = new List<TeacherCoursePreference>(),
                TeacherAvailabilities = new List<TeacherAvailability>(),
                ClassroomAvailabilities = new List<ClassroomAvailability>(),
                Prerequisites = new List<CoursePrerequisite>(),
                Constraints = new List<IConstraint>() // 初始化为空以避免NullReferenceException
            };

            // 创建1个时间槽
            problem.TimeSlots.Add(new TimeSlotInfo
            {
                Id = 1,
                DayOfWeek = 1,
                DayName = "Monday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(9, 30, 0)
            });

            // 创建1个教室
            problem.Classrooms.Add(new ClassroomInfo
            {
                Id = 1,
                Name = "Room 101",
                Building = "Building A",
                CampusId = 1,
                CampusName = "Main Campus",
                Capacity = 100, // 确保足够大
                Type = "Regular",
                HasComputers = true,
                HasProjector = true
            });

            // 创建1位教师
            problem.Teachers.Add(new TeacherInfo
            {
                Id = 1,
                Name = "Professor Smith",
                Title = "Professor",
                DepartmentId = 1,
                DepartmentName = "Computer Science",
                MaxWeeklyHours = 40,
                MaxDailyHours = 8,
                MaxConsecutiveHours = 4
            });

            // 创建1个课程班级
            problem.CourseSections.Add(new CourseSectionInfo
            {
                Id = 1,
                CourseId = 1,
                CourseCode = "CS101",
                CourseName = "Introduction to Programming",
                SectionCode = "CS101-A",
                Credits = 3,
                Hours = 6,
                Enrollment = 50,
                MaxEnrollment = 60,
                DepartmentId = 1,
                DepartmentName = "Computer Science",
                CourseType = "Regular",
                RequiredRoomType = "Regular"
            });

            // 添加教师课程偏好
            problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
            {
                TeacherId = 1,
                CourseId = 1,
                ProficiencyLevel = 5, // 最高能力值
                PreferenceLevel = 5   // 最高偏好
            });

            return problem;
        }
        public SchedulingProblem CreateDebugFeasibleProblem()
        {
            // 创建基本问题结构
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Debug Feasible Problem",
                SemesterId = 1,
                CourseSections = new List<CourseSectionInfo>(),
                Teachers = new List<TeacherInfo>(),
                Classrooms = new List<ClassroomInfo>(),
                TimeSlots = new List<TimeSlotInfo>(),
                TeacherCoursePreferences = new List<TeacherCoursePreference>(),
                TeacherAvailabilities = new List<TeacherAvailability>(),
                ClassroomAvailabilities = new List<ClassroomAvailability>(),
                Prerequisites = new List<CoursePrerequisite>(),
                Constraints = new List<IConstraint>()
            };

            // 创建3个时间槽 (每天1个)
            for (int day = 1; day <= 3; day++)
            {
                problem.TimeSlots.Add(new TimeSlotInfo
                {
                    Id = day,
                    DayOfWeek = day,
                    DayName = $"Day {day}",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0)
                });
            }

            // 创建3个足够大的教室 
            for (int i = 1; i <= 3; i++)
            {
                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"Room {i}",
                    Building = "Main Building",
                    CampusId = 1,
                    CampusName = "Main Campus",
                    Capacity = 100, // 确保足够大容量
                    Type = "Regular",
                    HasComputers = true,
                    HasProjector = true
                });
            }

            // 创建3位教师
            for (int i = 1; i <= 3; i++)
            {
                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"Teacher {i}",
                    Title = "Professor",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    MaxWeeklyHours = 40,
                    MaxDailyHours = 8,
                    MaxConsecutiveHours = 4
                });
            }

            // 创建3个课程班级，确保课程ID和班级ID一致 (简化调试)
            for (int i = 1; i <= 3; i++)
            {
                problem.CourseSections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = i,
                    CourseCode = $"CS10{i}",
                    CourseName = $"Course {i}",
                    SectionCode = $"CS10{i}-A",
                    Credits = 3,
                    Hours = 6,
                    Enrollment = 30, // 低于所有教室容量
                    MaxEnrollment = 40,
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    CourseType = "Regular",
                    RequiredRoomType = "Regular"
                });
            }

            // 添加教师课程偏好 - 每位教师可以教授所有课程，能力值和偏好都是最高的
            foreach (var teacher in problem.Teachers)
            {
                foreach (var course in problem.CourseSections)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = course.CourseId,
                        ProficiencyLevel = 5, // 最高能力值
                        PreferenceLevel = 5   // 最高偏好
                    });
                }
            }

            // 确保没有不可用的教师时间段
            // (不添加任何TeacherAvailability记录，默认为所有时间都可用)

            // 确保没有不可用的教室时间段
            // (不添加任何ClassroomAvailability记录，默认为所有时间都可用)

            // 不添加任何先修课程关系，避免不必要的复杂性

            return problem;
        }
    }
}