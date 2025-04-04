using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class ConstraintSettingDto
    {
        public int ConstraintId { get; set; }
        public bool IsActive { get; set; }
        public double Weight { get; set; }
    }
}
