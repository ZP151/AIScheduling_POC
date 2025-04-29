using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class CourseSection
    {
        public int CourseSectionId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }
        public string SectionCode { get; set; }
        public int MaxEnrollment { get; set; }
        public string CourseType { get; set; }                      // 如 "Lab", "Computer"
        public List<EquipmentType> RequiredEquipment { get; set; }
        public int ExpectedStudentCount { get; set; }     // 课程预计学生人数
        public enum EquipmentType
        {
            Projector,
            Computer,
            Whiteboard,
            Microphone,
            Speaker,
            LabBench,
            SmartBoard
        }
        public int ActualEnrollment { get; set; }
        public string GenderRestriction {  get; set; }
        // 导航属性
        public Course Course { get; set; }
        public Semester Semester { get; set; }
        public ICollection<ScheduleResult> ScheduleResults { get; set; }
    }
}
