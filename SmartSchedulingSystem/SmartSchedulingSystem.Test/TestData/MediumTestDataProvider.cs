using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;
using System;
using System.Collections.Generic;
using SmartSchedulingSystem.Scheduling.Utils;
namespace SmartSchedulingSystem.Test.TestData
{
    public static class MediumTestDataProvider
    {
        private static readonly Random _random = new Random();
        public static SchedulingProblem CreateMediumTestProblem2()
        {
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Medium Test Problem",
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

            // 添加时间槽 - 确保至少有8个
            for (int i = 1; i <= 8; i++)
            {
                problem.TimeSlots.Add(new TimeSlotInfo
                {
                    Id = i,
                    DayOfWeek = ((i - 1) / 4) + 1, // 分布在2天
                    StartTime = new TimeSpan(8 + ((i - 1) % 4) * 2, 0, 0),
                    EndTime = new TimeSpan(9 + ((i - 1) % 4) * 2, 0, 0)
                });
            }

            // 添加教室 - 确保至少有4个
            for (int i = 1; i <= 4; i++)
            {
                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"Room {i}",
                    Building = "Main Building",
                    Capacity = 50
                });
            }

            // 添加教师 - 确保至少有4个
            for (int i = 1; i <= 4; i++)
            {
                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"Teacher {i}",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science"
                });
            }

            // 添加课程 - 限制为4门
            for (int i = 1; i <= 4; i++)
            {
                problem.CourseSections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = i,
                    CourseCode = $"CS10{i}",
                    CourseName = $"Course {i}",
                    SectionCode = $"CS10{i}-A",
                    Enrollment = 30, // 小于教室容量
                });
            }

            // 添加教师课程偏好 - 确保每个教师可以教授每门课
            foreach (var teacher in problem.Teachers)
            {
                foreach (var course in problem.CourseSections)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = course.CourseId,
                        ProficiencyLevel = 5, // 最高水平
                        PreferenceLevel = 5   // 最高偏好
                    });
                }
            }

            // 添加调试信息
            Console.WriteLine($"生成了测试问题: 课程数={problem.CourseSections.Count}, " +
                             $"教师数={problem.Teachers.Count}, 教室数={problem.Classrooms.Count}, " +
                             $"时间槽数={problem.TimeSlots.Count}");
            Console.WriteLine($"教师课程偏好数={problem.TeacherCoursePreferences.Count}");

            return problem;
        }
        public static SchedulingProblem CreateMediumTestProblem()
        {
            // 创建排课问题 - 只有4个课程、5个教师、3个教室
            var problem = new SchedulingProblem
            {
                Id = 2,
                Name = "Medium Test Problem",
                SemesterId = 1
            };
            // 生成标准时间槽
            problem.TimeSlots = TestDataGenerator.GenerateStandardTimeSlots();

            // 添加教室 - 确保至少有4个
            for (int i = 1; i <= 4; i++)
            {
                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"Room {i}",
                    Building = "Main Building",
                    Capacity = 50
                });
            }
            // 生成教师
            for (int i = 1; i <= 5; i++)
            {
                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"Teacher {i}",
                    Title = "Professor",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    MaxWeeklyHours = 20, // 最大周课时
                    MaxDailyHours = 8    // 最大日课时
                });
            }

            // 生成课程班级
            for (int i = 1; i <= 4; i++)
            {
                int weeklyHours = 2 * (_random.Next(1, 4)); // 2/4/6 小时任选
                int sessionsPerWeek = weeklyHours / 2;      // 每节课 2 小时
                double hoursPerSession = 2;

                problem.CourseSections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = i,
                    CourseCode = $"CS10{i}",
                    CourseName = $"Course {i}",
                    SectionCode = $"CS10{i}-A",
                    Credits = 2,
                    WeeklyHours = weeklyHours,
                    SessionsPerWeek = sessionsPerWeek,
                    HoursPerSession = hoursPerSession,
                    Enrollment = 30,
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    CourseType = "Regular",
                    RequiredRoomType = "Regular"
                });
            }

            // 添加教师课程偏好
            foreach (var teacher in problem.Teachers)
            {
                foreach (var course in problem.CourseSections)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = course.CourseId,
                        ProficiencyLevel = 5,
                        PreferenceLevel = 5
                    });
                }
            }

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