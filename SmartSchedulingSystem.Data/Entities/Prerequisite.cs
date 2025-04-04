using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class Prerequisite
    {
        public int CourseId { get; set; }
        public int PrerequisiteCourseId { get; set; }
        // ✅ 导航属性
        public Course Course { get; set; }                // 当前课程
        public Course PrerequisiteCourse { get; set; }    // 被依赖的先修课程
    }
}
