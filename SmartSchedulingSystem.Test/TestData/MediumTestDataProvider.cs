using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;
using System;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Test.TestData
{
    public static class MediumTestDataProvider
    {
        public static SchedulingProblem CreateMediumTestProblem()
        {
            // 创建排课问题 - 只有8个课程、5个教师、3个教室、12个时间槽
            var problem = new SchedulingProblem
            {
                Id = 2,
                Name = "Medium Test Problem",
                SemesterId = 1
            };

            // 时间槽
            problem.TimeSlots = new List<TimeSlotInfo>
            {
                new TimeSlotInfo { Id = 1, DayOfWeek = 1, DayName = "Monday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 2, DayOfWeek = 1, DayName = "Monday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 3, DayOfWeek = 2, DayName = "Tuesday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 4, DayOfWeek = 2, DayName = "Tuesday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 5, DayOfWeek = 3, DayName = "Wednesday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 6, DayOfWeek = 3, DayName = "Wednesday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 7, DayOfWeek = 4, DayName = "Thursday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 8, DayOfWeek = 4, DayName = "Thursday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 9, DayOfWeek = 5, DayName = "Friday", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 10, DayOfWeek = 5, DayName = "Friday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 11, DayOfWeek = 5, DayName = "Friday", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Type = "Regular" },
                new TimeSlotInfo { Id = 12, DayOfWeek = 5, DayName = "Friday", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 30, 0), Type = "Regular" }
            };

            // 教室
            problem.Classrooms = new List<ClassroomInfo>
            {
                new ClassroomInfo { Id = 1, Name = "A101", Building = "A", CampusId = 1, CampusName = "Main Campus", Capacity = 60, Type = "Regular", HasProjector = true, HasComputers = true },
                new ClassroomInfo { Id = 2, Name = "B202", Building = "B", CampusId = 1, CampusName = "Main Campus", Capacity = 40, Type = "Regular", HasProjector = true, HasComputers = true },
                new ClassroomInfo { Id = 3, Name = "C303", Building = "C", CampusId = 1, CampusName = "Main Campus", Capacity = 100, Type = "Lecture", HasProjector = true, HasComputers = true }
            };

            // 教师
            problem.Teachers = new List<TeacherInfo>
            {
                new TeacherInfo { Id = 1, Name = "Dr. Wu", Title = "Professor", DepartmentId = 1, DepartmentName = "CS", MaxWeeklyHours = 16, MaxDailyHours = 6, MaxConsecutiveHours = 3 },
                new TeacherInfo { Id = 2, Name = "Dr. Chen", Title = "Associate Prof.", DepartmentId = 1, DepartmentName = "CS", MaxWeeklyHours = 14, MaxDailyHours = 6, MaxConsecutiveHours = 3 },
                new TeacherInfo { Id = 3, Name = "Dr. Zhang", Title = "Lecturer", DepartmentId = 2, DepartmentName = "Math", MaxWeeklyHours = 12, MaxDailyHours = 6, MaxConsecutiveHours = 2 },
                new TeacherInfo { Id = 4, Name = "Ms. Li", Title = "Assistant", DepartmentId = 2, DepartmentName = "Math", MaxWeeklyHours = 10, MaxDailyHours = 6, MaxConsecutiveHours = 2 },
                new TeacherInfo { Id = 5, Name = "Mr. Wang", Title = "Lecturer", DepartmentId = 3, DepartmentName = "Eng", MaxWeeklyHours = 10, MaxDailyHours = 6, MaxConsecutiveHours = 2 }
            };

            // 课程
            problem.CourseSections = new List<CourseSectionInfo>
            {
                new CourseSectionInfo { Id = 1, CourseId = 1, CourseCode = "CS101", CourseName = "编程基础", SectionCode = "CS101-A", Credits = 3, Hours = 3, Enrollment = 45,  DepartmentId = 1, DepartmentName = "CS", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" },
                new CourseSectionInfo { Id = 2, CourseId = 2, CourseCode = "CS102", CourseName = "数据结构", SectionCode = "CS102-A", Credits = 3, Hours = 3, Enrollment = 40,  DepartmentId = 1, DepartmentName = "CS", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" },
                new CourseSectionInfo { Id = 3, CourseId = 3, CourseCode = "CS201", CourseName = "算法设计", SectionCode = "CS201-A", Credits = 3, Hours = 3, Enrollment = 35,  DepartmentId = 1, DepartmentName = "CS", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" },
                new CourseSectionInfo { Id = 4, CourseId = 4, CourseCode = "MATH101", CourseName = "离散数学", SectionCode = "MATH101-A", Credits = 3, Hours = 3, Enrollment = 50, DepartmentId = 2, DepartmentName = "Math", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" },
                new CourseSectionInfo { Id = 5, CourseId = 5, CourseCode = "MATH201", CourseName = "线性代数", SectionCode = "MATH201-A", Credits = 3, Hours = 3, Enrollment = 40, DepartmentId = 2, DepartmentName = "Math", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" },
                new CourseSectionInfo { Id = 6, CourseId = 6, CourseCode = "ENG101", CourseName = "学术英语", SectionCode = "ENG101-A", Credits = 2, Hours = 2, Enrollment = 30,  DepartmentId = 3, DepartmentName = "Eng", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" },
                new CourseSectionInfo { Id = 7, CourseId = 7, CourseCode = "CS301", CourseName = "人工智能导论", SectionCode = "CS301-A", Credits = 3, Hours = 3, Enrollment = 30, DepartmentId = 1, DepartmentName = "CS", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" },
                new CourseSectionInfo { Id = 8, CourseId = 8, CourseCode = "CS401", CourseName = "深度学习", SectionCode = "CS401-A", Credits = 3, Hours = 3, Enrollment = 30, DepartmentId = 1, DepartmentName = "CS", CourseType = "Regular", RequiredRoomType = "Regular", RequiredEquipment = "Projector" }
            };

            // 教师课程能力
            problem.TeacherCoursePreferences = new List<TeacherCoursePreference>
            {
                new TeacherCoursePreference { TeacherId = 1, CourseId = 1, ProficiencyLevel = 5, PreferenceLevel = 5 },
                new TeacherCoursePreference { TeacherId = 1, CourseId = 2, ProficiencyLevel = 5, PreferenceLevel = 5 },
                new TeacherCoursePreference { TeacherId = 2, CourseId = 7, ProficiencyLevel = 5, PreferenceLevel = 4 },
                new TeacherCoursePreference { TeacherId = 2, CourseId = 8, ProficiencyLevel = 4, PreferenceLevel = 3 },
                new TeacherCoursePreference { TeacherId = 2, CourseId = 3, ProficiencyLevel = 5, PreferenceLevel = 4 },
                new TeacherCoursePreference { TeacherId = 3, CourseId = 4, ProficiencyLevel = 5, PreferenceLevel = 5 },
                new TeacherCoursePreference { TeacherId = 4, CourseId = 5, ProficiencyLevel = 4, PreferenceLevel = 4 },
                new TeacherCoursePreference { TeacherId = 5, CourseId = 6, ProficiencyLevel = 5, PreferenceLevel = 5 }
            };

            // 教师可用性
            problem.TeacherAvailabilities = new List<TeacherAvailability>();
            foreach (var teacher in problem.Teachers)
            {
                foreach (var slot in problem.TimeSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = true
                    });
                }
            }
            //problem.TeacherAvailabilities = new List<TeacherAvailability>
            //{
            //    new TeacherAvailability { TeacherId = 1, TimeSlotId = 1, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 1, TimeSlotId = 2, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 2, TimeSlotId = 3, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 2, TimeSlotId = 4, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 2, TimeSlotId = 5, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 3, TimeSlotId = 6, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 4, TimeSlotId = 7, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 5, TimeSlotId = 8, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 2, TimeSlotId = 9, IsAvailable = true },
            //    new TeacherAvailability { TeacherId = 2, TimeSlotId = 10, IsAvailable = true }
            //};

            // 教室可用性
            problem.ClassroomAvailabilities = new List<ClassroomAvailability>
            {
                new ClassroomAvailability { ClassroomId = 1, TimeSlotId = 1, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 1, TimeSlotId = 2, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 1, TimeSlotId = 3, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 2, TimeSlotId = 4, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 2, TimeSlotId = 5, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 2, TimeSlotId = 6, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 3, TimeSlotId = 7, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 3, TimeSlotId = 8, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 3, TimeSlotId = 9, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 3, TimeSlotId = 10, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 3, TimeSlotId = 11, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 3, TimeSlotId = 12, IsAvailable = true }
            };

            // 先修关系
            problem.Prerequisites = new List<CoursePrerequisite>
            {
                new CoursePrerequisite { CourseId = 2, PrerequisiteCourseId = 0 },
                new CoursePrerequisite { CourseId = 3, PrerequisiteCourseId = 0 },
                new CoursePrerequisite { CourseId = 5, PrerequisiteCourseId = 0 },
                new CoursePrerequisite { CourseId = 7, PrerequisiteCourseId = 0 },
                new CoursePrerequisite { CourseId = 8, PrerequisiteCourseId = 0 }
            };

            return problem;
        }
    }
}