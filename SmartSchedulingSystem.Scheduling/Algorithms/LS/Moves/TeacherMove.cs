using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// 表示更改课程教师的移动
    /// </summary>
    public class TeacherMove : IMove
    {
        private readonly int _assignmentId;
        private readonly int _newTeacherId;

        public TeacherMove(int assignmentId, int newTeacherId)
        {
            _assignmentId = assignmentId;
            _newTeacherId = newTeacherId;
        }
        public int NewTeacherId => _newTeacherId;
        public int AssignmentId => _assignmentId;
        public SchedulingSolution Apply(SchedulingSolution solution)
        {
            // 创建解决方案的深拷贝
            var newSolution = solution.Clone();

            // 查找要修改的分配
            var assignment = newSolution.Assignments.FirstOrDefault(a => a.Id == _assignmentId);
            if (assignment == null)
            {
                return newSolution; // 未找到分配，返回未修改的解
            }

            // 更新教师
            assignment.TeacherId = _newTeacherId;

            return newSolution;
        }

        public string GetDescription()
        {
            return $"将课程分配 #{_assignmentId} 分配给教师 #{_newTeacherId}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignmentId };
        }
    }
}