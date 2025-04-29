using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class Campus
    {
        public int CampusId { get; set; }
        public string Name { get; set; }

        public ICollection<Building> Buildings { get; set; }
        public ICollection<CampusTravelTime> FromCampusTravelTimes { get; set; }
        public ICollection<CampusTravelTime> ToCampusTravelTimes { get; set; }
    }
}
