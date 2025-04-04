using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// 排课分配，表示一次课程安排
    /// </summary>
    public class SchedulingAssignment
    {
        /// <summary>
        /// 排课分配的唯一ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 课程班级ID
        /// </summary>
        public int SectionId { get; set; }

        /// <summary>
        /// 课程班级代码
        /// </summary>
        public string SectionCode { get; set; }

        /// <summary>
        /// 教师ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// 教师名称
        /// </summary>
        public string TeacherName { get; set; }

        /// <summary>
        /// 教室ID
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// 教室名称
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// 时间槽ID
        /// </summary>
        public int TimeSlotId { get; set; }

        /// <summary>
        /// 星期几（1-7）
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
        /// 适用的教学周（例如[1,2,3,4,5,6,7,8,9,10,11,12]）
        /// </summary>
        public List<int> WeekPattern { get; set; } = new List<int>();

 
    }
}
