using System;
using System.Collections.Generic;
using System.Data;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Data.Entities;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    
    /// 表示一个待解决的排课问题，包含所有必要的输入数据
    /// </summary>
    public class SchedulingProblem
    {
        
        /// 问题的唯一ID
        public int Id { get; set; }
        
        /// 问题名称或描述
        public string Name { get; set; }

        /// 学期ID

        public int SemesterId { get; set; }

        /// 要排课的课程班级列表
        public List<CourseSectionInfo> CourseSections { get; set; } = new List<CourseSectionInfo>();

        /// 可用的教师列表
        public List<TeacherInfo> Teachers { get; set; } = new List<TeacherInfo>();

        /// 可用的教室列表
        public List<ClassroomInfo> Classrooms { get; set; } = new List<ClassroomInfo>();

        /// 可用的时间槽列表
        public List<TimeSlotInfo> TimeSlots { get; set; } = new List<TimeSlotInfo>();

        
        /// 教师课程能力和偏好映射
        public List<TeacherCoursePreference> TeacherCoursePreferences { get; set; } = new List<TeacherCoursePreference>();

        /// 教师的时间可用性

        public List<TeacherAvailability> TeacherAvailabilities { get; set; } = new List<TeacherAvailability>();
        
        /// 教室的时间可用性
        public List<ClassroomAvailability> ClassroomAvailabilities { get; set; } = new List<ClassroomAvailability>();
        /// <summary>
        /// 排课约束列表
        /// </summary>
        public List<IConstraint> Constraints { get; set; } = new List<IConstraint>();
        /// 是否需要生成多种排课方案
        public bool GenerateAlternatives { get; set; }

        /// 要生成的替代排课方案数量
        public int AlternativeCount { get; set; } = 3;
        public List<CoursePrerequisite> Prerequisites { get;  set; }

        /// 根据输入的基本数据校验问题的有效性
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
    
    /// 课程班级信息
    /// </summary>
    public class CourseSectionInfo
    {
        
        /// 班级ID

        public int Id { get; set; }

        
        /// 课程ID

        public int CourseId { get; set; }
        public Course Course { get; set; }


        /// 课程代码

        public string CourseCode { get; set; }

        
        /// 课程名称

        public string CourseName { get; set; }

        
        /// 班级代码

        public string SectionCode { get; set; }

        
        /// 学分

        public int Credits { get; set; }

        
        /// 学时

        public int Hours { get; set; }

        
        /// 选课学生人数

        public int Enrollment { get; set; }

        
        /// 最大容量

        public int MaxEnrollment { get; set; }

        
        /// 部门ID

        public int DepartmentId { get; set; }

        
        /// 部门名称

        public string DepartmentName { get; set; }

        
        /// 课程类型（理论、实验、实践等）

        public string CourseType { get; set; }

        
        /// 必要的教室类型

        public string RequiredRoomType { get; set; }

        
        /// 需要的教室设备（逗号分隔的列表）

        public string RequiredEquipment { get; set; }

        
        /// 是否要求相同的教师

        public bool RequiresSameTeacher { get; set; }

        
        /// 是否要求相同的教室

        public bool RequiresSameRoom { get; set; }

        
        
        /// 课程的跨列课程ID（如果有）

        public int? CrossListedWithId { get; set; }
    }

    
    /// 教师信息
    /// </summary>
    public class TeacherInfo
    {
        
        /// 教师ID

        public int Id { get; set; }

        
        /// 教师姓名

        public string Name { get; set; }

        
        /// 职称

        public string Title { get; set; }

        
        /// 部门ID

        public int DepartmentId { get; set; }

        
        /// 部门名称

        public string DepartmentName { get; set; }

        
        /// 最大周课时

        public int MaxWeeklyHours { get; set; }

        
        /// 最大每日课时

        public int MaxDailyHours { get; set; }

        
        /// 最大连续课时

        public int MaxConsecutiveHours { get; set; }

        
        /// 偏好的教学楼（可选）

        public string PreferredBuilding { get; set; }
    }

    
    /// 教室信息
    /// </summary>
    public class ClassroomInfo
    {
        
        /// 教室ID

        public int Id { get; set; }

        
        /// 教室名称

        public string Name { get; set; }

        
        /// 所在建筑物

        public string Building { get; set; }

        
        /// 校区ID

        public int CampusId { get; set; }

        
        /// 校区名称

        public string CampusName { get; set; }

        
        /// 容量

        public int Capacity { get; set; }

        
        /// 教室类型

        public string Type { get; set; }

        
        /// 设备（逗号分隔的列表）

        public string Equipment { get; set; }

        
        /// 是否有计算机

        public bool HasComputers { get; set; }

        
        /// 是否有投影仪

        public bool HasProjector { get; set; }
    }

    
    /// 时间槽信息
    /// </summary>
    public class TimeSlotInfo
    {
        
        /// 时间槽ID

        public int Id { get; set; }

        
        /// 星期几（1-7，1表示周一）

        public int DayOfWeek { get; set; }

        
        /// 星期几的文本表示

        public string DayName { get; set; }

        
        /// 开始时间

        public TimeSpan StartTime { get; set; }

        
        /// 结束时间

        public TimeSpan EndTime { get; set; }

        
        /// 时间段名称（例如："周一 8:00-9:30"）

        public string Display => $"{DayName} {StartTime:hh\\:mm}-{EndTime:hh\\:mm}";

        
        /// 时间段类型（正常、斋月等）

        public string Type { get; set; } = "Regular";
    }
    /// <summary>
    /// 表示课程之间的先修关系（Course A 是 Course B 的先修）
    /// </summary>
    public class CoursePrerequisite
    {
        public int id { get; set; }
        /// <summary>
        /// 后续课程的 ID（被约束的课程）
        /// </summary>
        public int CourseId { get; set; }
        public Course Course { get; set; }                // 当前课程

        /// <summary>
        /// 先修课程的 ID（必须先修完这个）
        /// </summary>
        public int PrerequisiteCourseId { get; set; }
    }


    /// 教师课程偏好
    /// </summary>
    public class TeacherCoursePreference
    {
        
        /// 教师ID

        public int TeacherId { get; set; }

        
        /// 课程ID

        public int CourseId { get; set; }

        
        /// 能力水平（1-5，5表示最高）

        public int ProficiencyLevel { get; set; }

        
        /// 偏好级别（1-5，5表示最喜欢）

        public int PreferenceLevel { get; set; }
    }

    
    /// 教师时间可用性
    /// </summary>
    public class TeacherAvailability
    {
        
        /// 教师ID

        public int TeacherId { get; set; }

        
        /// 时间槽ID

        public int TimeSlotId { get; set; }

        
        /// 是否可用

        public bool IsAvailable { get; set; }

        
        /// 偏好级别（1-5，5表示最喜欢）

        public int PreferenceLevel { get; set; }

        
        /// 适用的教学周

        public List<int> ApplicableWeeks { get; set; } = new List<int>();
    }

    
    /// 教室时间可用性
    /// </summary>
    public class ClassroomAvailability
    {
        
        /// 教室ID

        public int ClassroomId { get; set; }

        
        /// 时间槽ID

        public int TimeSlotId { get; set; }

        
        /// 是否可用

        public bool IsAvailable { get; set; }

        
        /// 不可用原因

        public string UnavailableReason { get; set; }

        
        /// 适用的教学周

        public List<int> ApplicableWeeks { get; set; } = new List<int>();
    }
}