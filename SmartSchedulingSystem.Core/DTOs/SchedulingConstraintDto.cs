using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class SchedulingConstraintDto
    {
        public int ConstraintId { get; set; }
        public string ConstraintType { get; set; }
        public string ConstraintName { get; set; }
        public string ConstraintDescription { get; set; }
        public double Weight { get; set; }
        public bool IsActive { get; set; }
    }
}
