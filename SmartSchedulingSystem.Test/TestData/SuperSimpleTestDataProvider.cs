using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;
using System;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Test.TestData
{
    public static class SuperSimpleTestDataProvider
    {
        public static SchedulingProblem CreateSuperSimpleTestProblem()
        {
            // 创建最简单的排课问题 - 只有1个课程、1个教师、1个教室、2个时间槽
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Super Simple Test Problem",
                SemesterId = 1
            };

            // 添加2个时间槽
            problem.TimeSlots = new List<TimeSlotInfo>
            {
                new TimeSlotInfo
                {
                    Id = 1,
                    DayOfWeek = 1,
                    DayName = "周一",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(9, 30, 0),
                    Type = "Regular"
                },
                new TimeSlotInfo
                {
                    Id = 2,
                    DayOfWeek = 1,
                    DayName = "周一",
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(11, 30, 0),
                    Type = "Regular"
                }
            };

            // 添加1个教室
            problem.Classrooms = new List<ClassroomInfo>
            {
                new ClassroomInfo
                {
                    Id = 1,
                    Name = "A101",
                    Building = "A",
                    CampusId = 1,
                    CampusName = "Main Campus",
                    Capacity = 50,
                    Type = "Regular",
                    Equipment = "Projector,Computer",
                    HasComputers = true,
                    HasProjector = true
                }
            };

            // 添加1个教师
            problem.Teachers = new List<TeacherInfo>
            {
                new TeacherInfo
                {
                    Id = 1,
                    Name = "张教授",
                    Title = "Professor",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    MaxWeeklyHours = 20,
                    MaxDailyHours = 6,
                    MaxConsecutiveHours = 3,
                    PreferredBuilding = "A"
                }
            };

            // 添加1个课程
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
                    Hours = 3,
                    Enrollment = 30,
                    MaxEnrollment = 40,
                    CourseType = "Regular",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    RequiredRoomType = "Regular",
                    RequiredEquipment = "Projector"
                }
            };

            // 设置教师课程能力 - 确保教师可以教授课程
            problem.TeacherCoursePreferences = new List<TeacherCoursePreference>
            {
                new TeacherCoursePreference
                {
                    TeacherId = 1,
                    CourseId = 1,
                    ProficiencyLevel = 5,
                    PreferenceLevel = 5
                }
            };

            // 明确设置所有可用性和约束（空而非null）
            problem.TeacherAvailabilities = new List<TeacherAvailability>();
            problem.ClassroomAvailabilities = new List<ClassroomAvailability>();
            problem.Prerequisites = new List<CoursePrerequisite>();
            problem.Constraints = new List<IConstraint>(); // 这个可能是关键

            return problem;
        }
    }
}