using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // 扩展版ClassroomDto，用于排课服务
    public class ClassroomExtDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Building { get; set; }
        public int Capacity { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public string Type { get; set; }
        public bool HasComputers { get; set; }
        public bool HasProjector { get; set; }
    }
} 