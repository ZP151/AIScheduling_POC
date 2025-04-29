using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{

    public class CampusTravelTime
    {
        public int FromCampusId { get; set; }
        public int ToCampusId { get; set; }
        public int Minutes { get; set; }

        // 可选导航属性（如你有 Campus 实体）
        public Campus FromCampus { get; set; }
        public Campus ToCampus { get; set; }
    }
}
