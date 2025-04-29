using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class Building
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CampusId { get; set; }

        // ✅ 导航属性
        public Campus Campus { get; set; }   // 你需要创建 Campus 实体类（如果你要表示校区）
        public ICollection<Classroom> Classrooms { get; set; }
    }

}
