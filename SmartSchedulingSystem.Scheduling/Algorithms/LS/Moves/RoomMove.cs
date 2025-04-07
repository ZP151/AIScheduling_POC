using SmartSchedulingSystem.Scheduling.Engine.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Engine.LS.Moves
{
    /// <summary>
    /// 表示更改课程教室的移动
    /// </summary>
    public class RoomMove : IMove
    {
        private readonly int _assignmentId;
        private readonly int _newClassroomId;

        public RoomMove(int assignmentId, int newClassroomId)
        {
            _assignmentId = assignmentId;
            _newClassroomId = newClassroomId;
        }
        public int NewClassroomId => _newClassroomId;
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

            // 更新教室
            assignment.ClassroomId = _newClassroomId;

            return newSolution;
        }

        public string GetDescription()
        {
            return $"将课程分配 #{_assignmentId} 移动到教室 #{_newClassroomId}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignmentId };
        }
    }
}