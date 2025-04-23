using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// 表示将课程安排移动到另一个时间槽的操作
    /// </summary>
    public class TimeSlotMove : IMove
    {
        private readonly int _assignmentId;
        private readonly int _newTimeSlotId;

        public TimeSlotMove(int assignmentId, int newTimeSlotId)
        {
            _assignmentId = assignmentId;
            _newTimeSlotId = newTimeSlotId;
        }

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

            // 查找新时间槽信息
            var timeSlot = newSolution.Problem.TimeSlots.FirstOrDefault(t => t.Id == _newTimeSlotId);
            if (timeSlot == null)
            {
                return newSolution; // 未找到新时间槽，返回未修改的解
            }

            // 更新时间槽相关属性
            assignment.TimeSlotId = _newTimeSlotId;
            assignment.DayOfWeek = timeSlot.DayOfWeek;
            assignment.StartTime = timeSlot.StartTime;
            assignment.EndTime = timeSlot.EndTime;

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