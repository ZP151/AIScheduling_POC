using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 表示CP算法生成的课程分配
    /// </summary>
    public class CourseAssignment
    {
        /// <summary>
        /// 课程班级信息
        /// </summary>
        public CourseSectionInfo Section { get; }
        
        /// <summary>
        /// 分配的教师
        /// </summary>
        public TeacherInfo Teacher { get; }
        
        /// <summary>
        /// 分配的教室
        /// </summary>
        public ClassroomInfo Classroom { get; }
        
        /// <summary>
        /// 分配的时间槽
        /// </summary>
        public TimeSlotInfo TimeSlot { get; }
        
        /// <summary>
        /// 创建新的课程分配
        /// </summary>
        public CourseAssignment(
            CourseSectionInfo section,
            TeacherInfo teacher,
            ClassroomInfo classroom,
            TimeSlotInfo timeSlot)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            Teacher = teacher ?? throw new ArgumentNullException(nameof(teacher));
            Classroom = classroom ?? throw new ArgumentNullException(nameof(classroom));
            TimeSlot = timeSlot ?? throw new ArgumentNullException(nameof(timeSlot));
        }
        
        /// <summary>
        /// 转换为通用的SchedulingAssignment对象
        /// </summary>
        public SchedulingAssignment ToSchedulingAssignment(SchedulingProblem problem)
        {
            return new SchedulingAssignment
            {
                SectionId = Section.Id,
                CourseSection = Section,
                TeacherId = Teacher.Id,
                Teacher = Teacher,
                ClassroomId = Classroom.Id,
                Classroom = Classroom,
                TimeSlotId = TimeSlot.Id,
                TimeSlot = TimeSlot
            };
        }
        
        /// <summary>
        /// 返回分配的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"课程:{Section.CourseName}(ID:{Section.Id}), 教师:{Teacher.Name}, 教室:{Classroom.Name}, 时间:{TimeSlot.DayName} {TimeSlot.StartTime}";
        }
    }
} 