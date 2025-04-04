using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class Classroom
    {
        public int ClassroomId { get; set; }
        public string Name { get; set; }
        public string Building { get; set; }
        public int Capacity { get; set; }
        public string RoomType { get; set; }
        public List<EquipmentType> AvailableEquipment { get; set; }

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


        // 导航属性
        public ICollection<ClassroomAvailability> Availabilities { get; set; }
        public ICollection<ScheduleResult> ScheduleResults { get; set; }
    }
}
