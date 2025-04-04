using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // 请求DTO
    public class ScheduleRequestDto
    {
        public int SemesterId { get; set; }
        public List<int> CourseSectionIds { get; set; }
        public List<int> TeacherIds { get; set; }
        public List<int> ClassroomIds { get; set; }
        public List<int> TimeSlotIds { get; set; }

        public bool UseAIAssistance { get; set; } = false;
        public List<ConstraintSettingDto> ConstraintSettings { get; set; }
    }
}
