using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class Teacher
    {
        public int TeacherId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int DepartmentId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        // 导航属性
        public Department Department { get; set; }
        public ICollection<TeacherAvailability> Availabilities { get; set; }
        public ICollection<ScheduleResult> ScheduleResults { get; set; }
        // ✅ 新增：支持 TeacherWorkloadConstraint
        public int MaxWeeklyHours { get; set; }
        public int MaxDailyHours { get; set; }
    }
}
