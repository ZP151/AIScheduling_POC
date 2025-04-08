using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// 表示更改课程时间的移动
    /// </summary>
    public class TimeMove : IMove
    {
        private readonly int _assignmentId;
        private readonly int _newTimeSlotId;

        public TimeMove(int assignmentId, int newTimeSlotId)
        {
            _assignmentId = assignmentId;
            _newTimeSlotId = newTimeSlotId;
        }
        public int NewTimeSlotId => _newTimeSlotId;
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

            // 更新时间槽
            assignment.TimeSlotId = _newTimeSlotId;

            return newSolution;
        }

        public string GetDescription()
        {
            return $"将课程分配 #{_assignmentId} 移动到时间槽 #{_newTimeSlotId}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignmentId };
        }
    }
}