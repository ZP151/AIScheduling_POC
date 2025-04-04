using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class SchedulingConstraint
    {
        public int ConstraintId { get; set; }
        public string ConstraintType { get; set; } // Hard, Soft
        public string ConstraintName { get; set; }
        public string ConstraintDescription { get; set; }
        public double Weight { get; set; }
        public bool IsActive { get; set; }
    }
}
