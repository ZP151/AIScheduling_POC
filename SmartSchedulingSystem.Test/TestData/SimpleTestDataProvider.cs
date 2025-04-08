using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Test.TestData
{
    public static class SimpleTestDataProvider
    {
        public static SchedulingProblem CreateSimpleTestProblem()
        {
            // 前面我提供的简单测试数据生成代码放在这里
            // 创建一个非常简单的排课问题
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Simple Test Problem",
                SemesterId = 1
            };

            // 创建时间槽 - 只创建2天，每天3个时间段
            problem.TimeSlots = new List<TimeSlotInfo>
            {
                new TimeSlotInfo { Id = 1, DayOfWeek = 1, DayName = "周一", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0) },
                new TimeSlotInfo { Id = 2, DayOfWeek = 1, DayName = "周一", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0) },
                new TimeSlotInfo { Id = 3, DayOfWeek = 1, DayName = "周一", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 30, 0) },
                new TimeSlotInfo { Id = 4, DayOfWeek = 2, DayName = "周二", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0) },
                new TimeSlotInfo { Id = 5, DayOfWeek = 2, DayName = "周二", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0) },
                new TimeSlotInfo { Id = 6, DayOfWeek = 2, DayName = "周二", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 30, 0) }
            };

            // 创建教室 - 只创建2个教室
            problem.Classrooms = new List<ClassroomInfo>
            {
                new ClassroomInfo { Id = 1, Name = "A101", Building = "A", CampusId = 1, CampusName = "Main Campus", Capacity = 50, Type = "Regular" },
                new ClassroomInfo { Id = 2, Name = "A102", Building = "A", CampusId = 1, CampusName = "Main Campus", Capacity = 30, Type = "Regular" }
            };

            // 创建教师 - 只创建2个教师
            problem.Teachers = new List<TeacherInfo>
            {
                new TeacherInfo { Id = 1, Name = "张教授", Title = "Professor", DepartmentId = 1, DepartmentName = "Computer Science", MaxWeeklyHours = 20, MaxDailyHours = 6 },
                new TeacherInfo { Id = 2, Name = "李教授", Title = "Professor", DepartmentId = 1, DepartmentName = "Computer Science", MaxWeeklyHours = 20, MaxDailyHours = 6 }
            };

            // 创建课程 - 只创建3个课程班级，确保数量少于时间槽*教室数，保证有解
            problem.CourseSections = new List<CourseSectionInfo>
            {
                new CourseSectionInfo
                {
                    Id = 1,
                    CourseId = 1,
                    CourseName = "程序设计基础",
                    CourseCode = "CS101",
                    SectionCode = "CS101-A",
                    Credits = 3,
                    Enrollment = 30
                },
                new CourseSectionInfo
                {
                    Id = 2,
                    CourseId = 2,
                    CourseName = "数据结构",
                    CourseCode = "CS102",
                    SectionCode = "CS102-A",
                    Credits = 3,
                    Enrollment = 25
                },
                new CourseSectionInfo
                {
                    Id = 3,
                    CourseId = 3,
                    CourseName = "计算机网络",
                    CourseCode = "CS103",
                    SectionCode = "CS103-A",
                    Credits = 3,
                    Enrollment = 20
                }
            };

            // 教师课程能力设置 - 确保每门课都有教师可教
            problem.TeacherCoursePreferences = new List<TeacherCoursePreference>
            {
                new TeacherCoursePreference { TeacherId = 1, CourseId = 1, ProficiencyLevel = 5, PreferenceLevel = 5 },
                new TeacherCoursePreference { TeacherId = 1, CourseId = 2, ProficiencyLevel = 5, PreferenceLevel = 4 },
                new TeacherCoursePreference { TeacherId = 2, CourseId = 2, ProficiencyLevel = 5, PreferenceLevel = 5 },
                new TeacherCoursePreference { TeacherId = 2, CourseId = 3, ProficiencyLevel = 5, PreferenceLevel = 5 }
            };

            // 不设置任何教师或教室不可用时间，默认全部可用
            problem.TeacherAvailabilities = new List<TeacherAvailability>();
            problem.ClassroomAvailabilities = new List<ClassroomAvailability>();

            // 不设置任何先修课程约束
            problem.Prerequisites = new List<CoursePrerequisite>();

            return problem;
        }
    }
}