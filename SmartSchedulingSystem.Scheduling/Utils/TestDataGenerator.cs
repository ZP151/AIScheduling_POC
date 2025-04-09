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
            GenerateCourseSectionsRealistic(problem, courseSectionCount);

            // 5. 创建更现实的教师课程偏好：每位教师只教1-3门课，每门课有1-3位教师
            CreateRealisticTeacherPreferences(problem);
            Console.WriteLine($"生成了 {problem.TeacherCoursePreferences.Count} 个教师课程偏好");

            // 6. 添加少量不可用时间段
            AddLimitedUnavailableTimes(problem);
            Console.WriteLine($"生成了 {problem.TeacherAvailabilities.Count} 个教师不可用时间段");
            Console.WriteLine($"生成了 {problem.ClassroomAvailabilities.Count} 个教室不可用时间段");


            return problem;
        }

        /// <summary>
        /// 生成测试时间槽
        /// </summary>
        // 生成合理的时间槽
        private void GenerateTimeSlots(SchedulingProblem problem, int count)
        {
            // 每天5个时间段，一周5天
            int slotsPerDay = 5;
            int maxDays = 5;

            int slotId = 1;
            for (int day = 1; day <= maxDays && slotId <= count; day++)
            {
                for (int slot = 0; slot < slotsPerDay && slotId <= count; slot++)
                {
                    // 8:00-9:30, 10:00-11:30, 13:00-14:30, 15:00-16:30, 18:30-20:00
                    TimeSpan startTime;
                    TimeSpan endTime;

                    switch (slot)
                    {
                        case 0: // 上午第一节
                            startTime = new TimeSpan(8, 0, 0);
                            endTime = new TimeSpan(9, 30, 0);
                            break;
                        case 1: // 上午第二节
                            startTime = new TimeSpan(10, 0, 0);
                            endTime = new TimeSpan(11, 30, 0);
                            break;
                        case 2: // 下午第一节
                            startTime = new TimeSpan(13, 0, 0);
                            endTime = new TimeSpan(14, 30, 0);
                            break;
                        case 3: // 下午第二节
                            startTime = new TimeSpan(15, 0, 0);
                            endTime = new TimeSpan(16, 30, 0);
                            break;
                        case 4: // 晚上
                            startTime = new TimeSpan(18, 30, 0);
                            endTime = new TimeSpan(20, 0, 0);
                            break;
                        default:
                            startTime = new TimeSpan(8 + slot * 2, 0, 0);
                            endTime = new TimeSpan(9 + slot * 2, 30, 0);
                            break;
                    }

                    problem.TimeSlots.Add(new TimeSlotInfo
                    {
                        Id = slotId,
                        DayOfWeek = day,
                        DayName = GetDayName(day),
                        StartTime = startTime,
                        EndTime = endTime
                    });

                    slotId++;
                }
            }
        }


        /// <summary>
        /// 生成测试教室
        /// </summary>
        private void GenerateClassrooms(SchedulingProblem problem, int count)
        {
            // 不同类型、不同容量的教室
            string[] types = { "Regular", "Computer", "Lab", "Lecture" };
            string[] buildings = { "A楼", "B楼", "C楼", "实验楼", "图书馆" };

            for (int i = 1; i <= count; i++)
            {
                string type = types[_random.Next(types.Length)];
                string building = buildings[_random.Next(buildings.Length)];

                // 根据教室类型设置合理的容量
                int capacity;
                bool hasComputers = false;
                bool hasProjector = _random.NextDouble() > 0.2; // 80%有投影仪

                switch (type)
                {
                    case "Regular":
                        capacity = _random.Next(30, 61); // 30-60人
                        break;
                    case "Computer":
                        capacity = _random.Next(20, 41); // 20-40人
                        hasComputers = true;
                        break;
                    case "Lab":
                        capacity = _random.Next(20, 36); // 20-35人
                        break;
                    case "Lecture":
                        capacity = _random.Next(80, 201); // 80-200人
                        break;
                    default:
                        capacity = _random.Next(30, 61);
                        break;
                }

                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"{building}-{100 + i}",
                    Building = building,
                    CampusId = _random.Next(1, 3), // 1或2，表示主校区或分校区
                    CampusName = _random.Next(1, 3) == 1 ? "主校区" : "分校区",
                    Capacity = capacity,
                    Type = type,
                    HasComputers = hasComputers,
                    HasProjector = hasProjector
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
            string[] surnames = { "张", "王", "李", "赵", "刘", "陈", "杨", "黄", "周", "吴", "郑", "马", "孙", "朱", "胡" };
            string[] titles = { "教授", "副教授", "讲师", "助教" };
            string[] departments = { "计算机科学", "数学", "物理", "工程", "经济", "管理", "外语" };

            for (int i = 1; i <= count; i++)
            {
                string surname = surnames[_random.Next(surnames.Length)];
                string title = titles[_random.Next(titles.Length)];
                int departmentId = _random.Next(1, departments.Length + 1);

                // 根据职称设置合理的工作负荷
                int maxWeeklyHours, maxDailyHours, maxConsecutiveHours;

                switch (title)
                {
                    case "教授":
                        maxWeeklyHours = _random.Next(10, 17); // 10-16小时
                        maxDailyHours = 6;
                        maxConsecutiveHours = 4;
                        break;
                    case "副教授":
                        maxWeeklyHours = _random.Next(12, 19); // 12-18小时
                        maxDailyHours = 6;
                        maxConsecutiveHours = 4;
                        break;
                    case "讲师":
                        maxWeeklyHours = _random.Next(14, 21); // 14-20小时
                        maxDailyHours = 8;
                        maxConsecutiveHours = 6;
                        break;
                    case "助教":
                        maxWeeklyHours = _random.Next(16, 23); // 16-22小时
                        maxDailyHours = 8;
                        maxConsecutiveHours = 6;
                        break;
                    default:
                        maxWeeklyHours = _random.Next(12, 21); // 12-20小时
                        maxDailyHours = 6;
                        maxConsecutiveHours = 4;
                        break;
                }

                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"{surname}{_random.Next(1, 10)}",
                    Title = title,
                    DepartmentId = departmentId,
                    DepartmentName = departments[departmentId - 1],
                    MaxWeeklyHours = maxWeeklyHours,
                    MaxDailyHours = maxDailyHours,
                    MaxConsecutiveHours = maxConsecutiveHours
                });
            }
        }

        // 生成课程班级，确保班级大小合理
        private void GenerateCourseSectionsRealistic(SchedulingProblem problem, int count)
        {
            string[] subjects = { "CS", "MATH", "PHYS", "ENG", "ECON", "MGT", "LANG" };
            string[] courseTypes = { "Regular", "Computer", "Lab", "Regular" }; // 普通课程更常见

            Dictionary<string, string[]> subjectCourses = new Dictionary<string, string[]>
            {
                { "CS", new[] { "程序设计", "数据结构", "算法", "数据库", "计算机网络", "操作系统", "软件工程", "人工智能" } },
                { "MATH", new[] { "高等数学", "线性代数", "概率论", "离散数学", "数值分析", "统计学", "运筹学" } },
                { "PHYS", new[] { "力学", "电磁学", "热学", "光学", "原子物理", "量子力学", "相对论" } },
                { "ENG", new[] { "工程力学", "材料力学", "电路原理", "信号与系统", "自动控制", "微电子技术" } },
                { "ECON", new[] { "微观经济学", "宏观经济学", "计量经济学", "金融学", "国际贸易", "财政学" } },
                { "MGT", new[] { "管理学原理", "市场营销", "人力资源", "财务管理", "战略管理", "运营管理" } },
                { "LANG", new[] { "英语", "日语", "德语", "法语", "俄语", "西班牙语", "汉语" } }
            };

            // 为每门课程生成1-3个班级
            int sectionId = 1;
            int courseId = 1;

            while (sectionId <= count)
            {
                string subject = subjects[_random.Next(subjects.Length)];
                string[] courseNames = subjectCourses[subject];
                string courseName = courseNames[_random.Next(courseNames.Length)];
                string courseType = courseTypes[_random.Next(courseTypes.Length)];

                int courseNumber = 100 + courseId;
                string courseCode = $"{subject}{courseNumber}";

                // 确定此课程有几个班级
                int sectionCount = Math.Min(_random.Next(1, 4), count - sectionId + 1); // 1-3个班级，但不超过剩余数量

                for (int section = 0; section < sectionCount; section++)
                {
                    char sectionChar = (char)('A' + section);

                    // 根据课程类型设置班级大小
                    int enrollment;
                    string requiredRoomType;

                    switch (courseType)
                    {
                        case "Regular":
                            enrollment = _random.Next(20, 61); // 20-60人
                            requiredRoomType = "Regular";
                            break;
                        case "Computer":
                            enrollment = _random.Next(15, 31); // 15-30人
                            requiredRoomType = "Computer";
                            break;
                        case "Lab":
                            enrollment = _random.Next(10, 26); // 10-25人
                            requiredRoomType = "Lab";
                            break;
                        default:
                            enrollment = _random.Next(20, 51);
                            requiredRoomType = "Regular";
                            break;
                    }

                    problem.CourseSections.Add(new CourseSectionInfo
                    {
                        Id = sectionId,
                        CourseId = courseId,
                        CourseCode = courseCode,
                        CourseName = courseName,
                        SectionCode = $"{courseCode}-{sectionChar}",
                        Credits = 3,
                        Hours = 6,
                        Enrollment = enrollment,
                        MaxEnrollment = enrollment + _random.Next(5, 11),
                        DepartmentId = _random.Next(1, 5),
                        DepartmentName = "学院" + _random.Next(1, 5),
                        CourseType = courseType,
                        RequiredRoomType = requiredRoomType
                    });

                    sectionId++;
                    if (sectionId > count) break;
                }

                courseId++;
            }
        }

        private void CreateRealisticTeacherPreferences(SchedulingProblem problem)
        {
            // 清除现有偏好
            problem.TeacherCoursePreferences.Clear();

            // 获取所有不同的课程ID
            var distinctCourseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .ToList();

            // 为每门课程分配1-3位教师
            foreach (var courseId in distinctCourseIds)
            {
                // 获取课程信息
                var course = problem.CourseSections.First(s => s.CourseId == courseId);

                // 确定这门课需要几位教师
                int teacherCount = _random.Next(1, 4); // 1-3位教师

                // 随机选择教师
                var selectedTeachers = problem.Teachers
                    .OrderBy(x => _random.Next())
                    .Take(teacherCount)
                    .ToList();

                foreach (var teacher in selectedTeachers)
                {
                    // 添加偏好，设置较高的能力值和偏好值
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = courseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5，确保有资格
                        PreferenceLevel = _random.Next(3, 6)   // 3-5，确保有较高偏好
                    });
                }
            }

            // 检查是否有教师没有分配课程
            var teachersWithoutCourses = problem.Teachers
                .Where(t => !problem.TeacherCoursePreferences.Any(p => p.TeacherId == t.Id))
                .ToList();

            // 为没有课程的教师随机分配1-2门课
            foreach (var teacher in teachersWithoutCourses)
            {
                int courseCount = _random.Next(1, 3); // 1-2门课

                var coursesToAssign = distinctCourseIds
                    .OrderBy(x => _random.Next())
                    .Take(courseCount);

                foreach (var courseId in coursesToAssign)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = courseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5
                        PreferenceLevel = _random.Next(3, 6)   // 3-5
                    });
                }
            }

            // 检查限制：确保每位教师最多教授3门不同课程
            foreach (var teacher in problem.Teachers)
            {
                var teacherCourses = problem.TeacherCoursePreferences
                    .Where(p => p.TeacherId == teacher.Id)
                    .Select(p => p.CourseId)
                    .Distinct()
                    .ToList();

                if (teacherCourses.Count > 3)
                {
                    // 如果超过3门，随机删除多余的课程
                    var coursesToKeep = teacherCourses
                        .OrderBy(x => _random.Next())
                        .Take(3)
                        .ToHashSet();

                    var prefsToRemove = problem.TeacherCoursePreferences
                        .Where(p => p.TeacherId == teacher.Id && !coursesToKeep.Contains(p.CourseId))
                        .ToList();

                    foreach (var pref in prefsToRemove)
                    {
                        problem.TeacherCoursePreferences.Remove(pref);
                    }
                }
            }
        }
        private void AddLimitedUnavailableTimes(SchedulingProblem problem)
        {
            // 清除现有的可用性设置
            problem.TeacherAvailabilities.Clear();
            problem.ClassroomAvailabilities.Clear();

            // 1. 为教师添加少量不可用时间段
            foreach (var teacher in problem.Teachers)
            {
                // 每个教师有1-2个不可用时间段
                int unavailableCount = _random.Next(1, 3);

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
            }

            // 2. 为教室添加少量不可用时间段
            foreach (var classroom in problem.Classrooms)
            {
                // 教室有0-1个不可用时间段
                int unavailableCount = _random.Next(2) == 0 ? 1 : 0;

                if (unavailableCount > 0)
                {
                    var unavailableSlot = problem.TimeSlots
                        .OrderBy(x => _random.Next())
                        .First();

                    problem.ClassroomAvailabilities.Add(new ClassroomAvailability
                    {
                        ClassroomId = classroom.Id,
                        TimeSlotId = unavailableSlot.Id,
                        IsAvailable = false,
                        UnavailableReason = _random.Next(2) == 0 ? "维护" : "预留"
                    });
                }
            }

            // 检查：确保我们不会添加太多约束导致问题无解
            EnsureFeasibility(problem);
        }

        /// <summary>
        /// 确保问题的可行性，避免添加太多导致无解的约束
        /// </summary>
        private void EnsureFeasibility(SchedulingProblem problem)
        {
            // 简单检查：确保每门课程至少有一种可行的安排方式
            foreach (var section in problem.CourseSections)
            {
                // 1. 找出可以教授这门课的教师
                var qualifiedTeachers = problem.TeacherCoursePreferences
                    .Where(p => p.CourseId == section.CourseId)
                    .Select(p => p.TeacherId)
                    .ToList();

                if (qualifiedTeachers.Count == 0)
                {
                    // 如果没有教师可以教，随机选择一位教师并添加偏好
                    var randomTeacher = problem.Teachers[_random.Next(problem.Teachers.Count)];

                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = randomTeacher.Id,
                        CourseId = section.CourseId,
                        ProficiencyLevel = 5,
                        PreferenceLevel = 4
                    });

                    qualifiedTeachers.Add(randomTeacher.Id);
                }

                // 2. 找出适合这门课的教室
                var suitableRooms = problem.Classrooms
                    .Where(r => r.Capacity >= section.Enrollment &&
                               (string.IsNullOrEmpty(section.RequiredRoomType) ||
                                r.Type == section.RequiredRoomType ||
                                section.RequiredRoomType == "Regular"))
                    .Select(r => r.Id)
                    .ToList();

                if (suitableRooms.Count == 0)
                {
                    // 如果没有合适的教室，修改一个教室使其适合
                    var largestRoom = problem.Classrooms
                        .OrderByDescending(r => r.Capacity)
                        .First();

                    largestRoom.Capacity = Math.Max(largestRoom.Capacity, section.Enrollment + 10);

                    if (!string.IsNullOrEmpty(section.RequiredRoomType) &&
                        section.RequiredRoomType != "Regular" &&
                        largestRoom.Type != section.RequiredRoomType)
                    {
                        largestRoom.Type = section.RequiredRoomType;
                    }

                    suitableRooms.Add(largestRoom.Id);
                }

                // 3. 确保每位合格教师在每个合适教室至少有一个可用时间段
                foreach (var teacherId in qualifiedTeachers)
                {
                    var unavailableTimeSlots = problem.TeacherAvailabilities
                        .Where(a => a.TeacherId == teacherId && !a.IsAvailable)
                        .Select(a => a.TimeSlotId)
                        .ToHashSet();

                    if (unavailableTimeSlots.Count >= problem.TimeSlots.Count)
                    {
                        // 如果教师所有时间都不可用，移除一些不可用性约束
                        var availabilitiesToRemove = problem.TeacherAvailabilities
                            .Where(a => a.TeacherId == teacherId)
                            .Take(problem.TeacherAvailabilities.Count - 3) // 确保至少有3个时间段可用
                            .ToList();

                        foreach (var a in availabilitiesToRemove)
                        {
                            problem.TeacherAvailabilities.Remove(a);
                        }
                    }
                }

                foreach (var roomId in suitableRooms)
                {
                    var unavailableTimeSlots = problem.ClassroomAvailabilities
                        .Where(a => a.ClassroomId == roomId && !a.IsAvailable)
                        .Select(a => a.TimeSlotId)
                        .ToHashSet();

                    if (unavailableTimeSlots.Count >= problem.TimeSlots.Count)
                    {
                        // 如果教室所有时间都不可用，移除一些不可用性约束
                        var availabilitiesToRemove = problem.ClassroomAvailabilities
                            .Where(a => a.ClassroomId == roomId)
                            .Take(problem.ClassroomAvailabilities.Count - 3) // 确保至少有3个时间段可用
                            .ToList();

                        foreach (var a in availabilitiesToRemove)
                        {
                            problem.ClassroomAvailabilities.Remove(a);
                        }
                    }
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