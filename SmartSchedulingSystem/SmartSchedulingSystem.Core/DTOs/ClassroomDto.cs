using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class ClassroomDto
    {
        public int ClassroomId { get; set; }
        public string Name { get; set; }
        public string Building { get; set; }
        public int Capacity { get; set; }
        public bool HasComputers { get; set; }
        public bool HasProjector { get; set; }
    }
}
