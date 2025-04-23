using System;
using System.Collections.Generic;
using System.Data;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Data.Entities;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// 表示一个待解决的排课问题，包含所有必要的输入数据
    /// </summary>
    public class SchedulingProblem
    {
        /// <summary>
        /// 问题的唯一ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 问题名称或描述
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 学期ID
        /// </summary>
        public int SemesterId { get; set; }

        /// <summary>
        /// 要排课的课程班级列表
        /// </summary>
        public List<CourseSectionInfo> CourseSections { get; set; } = new List<CourseSectionInfo>();

        /// <summary>
        /// 可用的教师列表
        /// </summary>
        public List<TeacherInfo> Teachers { get; set; } = new List<TeacherInfo>();

        /// <summary>
        /// 可用的教室列表
        /// </summary>
        public List<ClassroomInfo> Classrooms { get; set; } = new List<ClassroomInfo>();

        /// <summary>
        /// 可用的时间槽列表
        /// </summary>
        public List<TimeSlotInfo> TimeSlots { get; set; } = new List<TimeSlotInfo>();

        /// <summary>
        /// 教师课程能力和偏好映射
        /// </summary>
        public List<TeacherCoursePreference> TeacherCoursePreferences { get; set; } = new List<TeacherCoursePreference>();

        /// <summary>
        /// 教师可用性
        /// </summary>
        public List<TeacherAvailability> TeacherAvailabilities { get; set; } = new List<TeacherAvailability>();
        
        /// <summary>
        /// 教室可用性
        /// </summary>
        public List<ClassroomAvailability> ClassroomAvailabilities { get; set; } = new List<ClassroomAvailability>();
        
        /// <summary>
        /// 排课约束列表
        /// </summary>
        public List<IConstraint> Constraints { get; set; } = new List<IConstraint>();
        
        /// <summary>
        /// 是否生成多个解决方案
        /// </summary>
        public bool GenerateMultipleSolutions { get; set; } = true;

        /// <summary>
        /// 要生成的排课方案数量
        /// </summary>
        public int SolutionCount { get; set; } = 3;
        
        /// <summary>
        /// 课程先修关系列表
        /// </summary>
        public List<CoursePrerequisite> Prerequisites { get;  set; }

        /// <summary>
        /// 根据输入的基本数据校验问题的有效性
        /// </summary>
        /// <returns>错误消息列表，如果为空则表示有效</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (CourseSections == null || CourseSections.Count == 0)
                errors.Add("No course sections to schedule");

            if (Teachers == null || Teachers.Count == 0)
                errors.Add("No teachers available");

            if (Classrooms == null || Classrooms.Count == 0)
                errors.Add("No classrooms available");

            if (TimeSlots == null || TimeSlots.Count == 0)
                errors.Add("No time slots defined");

            // 校验教师与课程的匹配性
            if (TeacherCoursePreferences != null && TeacherCoursePreferences.Count > 0)
            {
                var teacherIds = Teachers.ConvertAll(t => t.Id);
                var courseIds = CourseSections.ConvertAll(c => c.CourseId);

                foreach (var preference in TeacherCoursePreferences)
                {
                    if (!teacherIds.Contains(preference.TeacherId))
                        errors.Add($"Teacher ID {preference.TeacherId} in preferences does not exist");

                    if (!courseIds.Contains(preference.CourseId))
                        errors.Add($"Course ID {preference.CourseId} in preferences does not exist");
                }
            }

            // 校验教师时间可用性
            if (TeacherAvailabilities != null && TeacherAvailabilities.Count > 0)
            {
                var teacherIds = Teachers.ConvertAll(t => t.Id);
                var timeSlotIds = TimeSlots.ConvertAll(t => t.Id);

                foreach (var availability in TeacherAvailabilities)
                {
                    if (!teacherIds.Contains(availability.TeacherId))
                        errors.Add($"Teacher ID {availability.TeacherId} in availabilities does not exist");

                    if (!timeSlotIds.Contains(availability.TimeSlotId))
                        errors.Add($"Time slot ID {availability.TimeSlotId} in teacher availabilities does not exist");
                }
            }

            // 校验教室时间可用性
            if (ClassroomAvailabilities != null && ClassroomAvailabilities.Count > 0)
            {
                var classroomIds = Classrooms.ConvertAll(c => c.Id);
                var timeSlotIds = TimeSlots.ConvertAll(t => t.Id);

                foreach (var availability in ClassroomAvailabilities)
                {
                    if (!classroomIds.Contains(availability.ClassroomId))
                        errors.Add($"Classroom ID {availability.ClassroomId} in availabilities does not exist");

                    if (!timeSlotIds.Contains(availability.TimeSlotId))
                        errors.Add($"Time slot ID {availability.TimeSlotId} in classroom availabilities does not exist");
                }
            }

            return errors;
        }
    }
    
    /// <summary>
    /// 课程班级信息
    /// </summary>
    public class CourseSectionInfo
    {
        /// <summary>
        /// 班级ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 课程ID
        /// </summary>
        public int CourseId { get; set; }
        public Course Course { get; set; }

        /// <summary>
        /// 课程代码
        /// </summary>
        public string CourseCode { get; set; }

        /// <summary>
        /// 课程名称
        /// </summary>
        public string CourseName { get; set; }

        /// <summary>
        /// 班级代码
        /// </summary>
        public string SectionCode { get; set; }

        /// <summary>
        /// 学分
        /// </summary>
        public int Credits { get; set; }

        /// <summary>
        /// 学时
        /// </summary>
        public int Hours { get; set; }
        
        /// <summary>
        /// 每周学时
        /// </summary>
        public int WeeklyHours { get; set; }

        /// <summary>
        /// 每周课次
        /// </summary>
        public int SessionsPerWeek { get; set; }

        /// <summary>
        /// 每次课时长（小时）
        /// </summary>
        public double HoursPerSession { get; set; }

        /// <summary>
        /// 该课学生容量
        /// 后面要该改变量名，改成该课程的学生容量
        /// </summary>
        public int Enrollment { get; set; }

        /// <summary>
        /// 部门ID
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// 课程类型（理论、实验、实践等）
        /// </summary>
        public string CourseType { get; set; }

        /// <summary>
        /// 必要的教室类型
        /// </summary>
        public string RequiredRoomType { get; set; }
        
        /// <summary>
        /// 必要的教室类型（与RequiredRoomType同义，用于兼容性）
        /// </summary>
        public string RequiredClassroomType { get => RequiredRoomType; set => RequiredRoomType = value; }

        /// <summary>
        /// 需要的教室设备（逗号分隔的列表）
        /// </summary>
        public string RequiredEquipment { get; set; }

        /// <summary>
        /// 是否要求相同的教师
        /// </summary>
        public bool RequiresSameTeacher { get; set; }

        /// <summary>
        /// 是否要求相同的教室
        /// </summary>
        public bool RequiresSameRoom { get; set; }

        /// <summary>
        /// 课程的跨列课程ID（如果有）
        /// </summary>
        public int? CrossListedWithId { get; set; }
        
        /// <summary>
        /// 课程难度级别（1-5，5表示最难）
        /// </summary>
        public int DifficultyLevel { get; set; } = 3;
    }

    /// <summary>
    /// 教师信息
    /// </summary>
    public class TeacherInfo
    {
        /// <summary>
        /// 教师ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 教师姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 职称
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 部门ID
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// 最大周课时
        /// </summary>
        public int MaxWeeklyHours { get; set; }

        /// <summary>
        /// 最大每日课时
        /// </summary>
        public int MaxDailyHours { get; set; }

        /// <summary>
        /// 最大连续课时
        /// </summary>
        public int MaxConsecutiveHours { get; set; }

        /// <summary>
        /// 偏好的教学楼（可选）
        /// </summary>
        public string PreferredBuilding { get; set; }
    }

    /// <summary>
    /// 教室信息
    /// </summary>
    public class ClassroomInfo
    {
        /// <summary>
        /// 教室ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 教室名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 所在建筑物
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// 校区ID
        /// </summary>
        public int CampusId { get; set; }

        /// <summary>
        /// 校区名称
        /// </summary>
        public string CampusName { get; set; }

        /// <summary>
        /// 容量
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// 教室类型
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// 教室类型（与Type同义，用于兼容性）
        /// </summary>
        public string ClassroomType { get => Type; set => Type = value; }
        
        /// <summary>
        /// 教室类型（与Type同义，用于兼容性）
        /// </summary>
        public string RoomType { get => Type; set => Type = value; }

        /// <summary>
        /// 设备（逗号分隔的列表）
        /// </summary>
        public string Equipment { get; set; }

        /// <summary>
        /// 是否有计算机
        /// </summary>
        public bool HasComputers { get; set; }

        /// <summary>
        /// 是否有投影仪
        /// </summary>
        public bool HasProjector { get; set; }
    }

    /// <summary>
    /// 时间槽信息
    /// </summary>
    public class TimeSlotInfo
    {
        /// <summary>
        /// 时间槽ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 星期几（1-7，1表示周一）
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// 星期几的文本表示
        /// </summary>
        public string DayName { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// 时间段名称（例如："周一 8:00-9:30"）
        /// </summary>
        public string Display => $"{DayName} {StartTime:hh\\:mm}-{EndTime:hh\\:mm}";

        /// <summary>
        /// 时间段类型（正常、斋月等）
        /// </summary>
        public string Type { get; set; } = "Regular";
    }
    /// <summary>
    /// 课程先修关系
    /// </summary>
    public class CoursePrerequisite
    {
        /// <summary>
        /// ID
        /// </summary>
        public int id { get; set; }
        
        /// <summary>
        /// 课程ID
        /// </summary>
        public int CourseId { get; set; }
        /// <summary>
        /// 课程对象
        /// </summary>
        public Course Course { get; set; }                // 当前课程
        
        /// <summary>
        /// 先修课程ID
        /// </summary>
        public int PrerequisiteCourseId { get; set; }
    }

    /// <summary>
    /// 教师课程偏好
    /// </summary>
    public class TeacherCoursePreference
    {
        /// <summary>
        /// 教师ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// 课程ID
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// 能力水平（1-5，5表示最高）
        /// </summary>
        public int ProficiencyLevel { get; set; }

        /// <summary>
        /// 偏好级别（1-5，5表示最喜欢）
        /// </summary>
        public int PreferenceLevel { get; set; }
    }

    /// <summary>
    /// 教师时间可用性
    /// </summary>
    public class TeacherAvailability
    {
        /// <summary>
        /// 教师ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// 教师姓名
        /// </summary>
        public string TeacherName { get; set; }

        /// <summary>
        /// 时间槽ID
        /// </summary>
        public int TimeSlotId { get; set; }

        /// <summary>
        /// 星期几
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 不可用原因
        /// </summary>
        public string UnavailableReason { get; set; }

        /// <summary>
        /// 偏好级别（1-5，5表示最喜欢）
        /// </summary>
        public int PreferenceLevel { get; set; }

        /// <summary>
        /// 适用的教学周
        /// </summary>
        public List<int> ApplicableWeeks { get; set; } = new List<int>();
    }

    /// <summary>
    /// 教室时间可用性
    /// </summary>
    public class ClassroomAvailability
    {
        /// <summary>
        /// 教室ID
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// 教室名称
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// 所在建筑物
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// 时间槽ID
        /// </summary>
        public int TimeSlotId { get; set; }

        /// <summary>
        /// 星期几
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 不可用原因
        /// </summary>
        public string UnavailableReason { get; set; }

        /// <summary>
        /// 适用的教学周
        /// </summary>
        public List<int> ApplicableWeeks { get; set; } = new List<int>();
    }
}