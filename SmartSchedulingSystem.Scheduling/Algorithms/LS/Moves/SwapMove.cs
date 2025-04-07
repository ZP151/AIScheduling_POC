using SmartSchedulingSystem.Scheduling.Engine.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Engine.LS.Moves
{
    /// <summary>
    /// 表示交换两个课程分配的移动
    /// </summary>
    public class SwapMove : IMove
    {
        private readonly int _assignmentId1;
        private readonly int _assignmentId2;
        private readonly bool _swapTime;
        private readonly bool _swapRoom;
        private readonly bool _swapTeacher;

        /// <summary>
        /// 创建一个新的交换移动
        /// </summary>
        /// <param name="assignmentId1">第一个分配ID</param>
        /// <param name="assignmentId2">第二个分配ID</param>
        /// <param name="swapTime">是否交换时间</param>
        /// <param name="swapRoom">是否交换教室</param>
        /// <param name="swapTeacher">是否交换教师</param>
        public SwapMove(int assignmentId1, int assignmentId2, bool swapTime = true, bool swapRoom = true, bool swapTeacher = false)
        {
            _assignmentId1 = assignmentId1;
            _assignmentId2 = assignmentId2;
            _swapTime = swapTime;
            _swapRoom = swapRoom;
            _swapTeacher = swapTeacher;
        }
        public int AssignmentId1 => _assignmentId1;
        public int AssignmentId2 => _assignmentId2;
        public bool SwapTime => _swapTime;
        public bool SwapRoom => _swapRoom;
        public bool SwapTeacher => _swapTeacher;

        public SchedulingSolution Apply(SchedulingSolution solution)
        {
            // 创建解决方案的深拷贝
            var newSolution = solution.Clone();

            // 查找要修改的分配
            var assignment1 = newSolution.Assignments.FirstOrDefault(a => a.Id == _assignmentId1);
            var assignment2 = newSolution.Assignments.FirstOrDefault(a => a.Id == _assignmentId2);

            if (assignment1 == null || assignment2 == null)
            {
                return newSolution; // 未找到分配，返回未修改的解
            }

            // 交换时间槽
            if (_swapTime)
            {
                int temp = assignment1.TimeSlotId;
                assignment1.TimeSlotId = assignment2.TimeSlotId;
                assignment2.TimeSlotId = temp;
            }

            // 交换教室
            if (_swapRoom)
            {
                int temp = assignment1.ClassroomId;
                assignment1.ClassroomId = assignment2.ClassroomId;
                assignment2.ClassroomId = temp;
            }

            // 交换教师
            if (_swapTeacher)
            {
                int temp = assignment1.TeacherId;
                assignment1.TeacherId = assignment2.TeacherId;
                assignment2.TeacherId = temp;
            }

            return newSolution;
        }

        public string GetDescription()
        {
            string swapItems = "";
            if (_swapTime) swapItems += "时间";
            if (_swapRoom)
            {
                if (!string.IsNullOrEmpty(swapItems)) swapItems += "和";
                swapItems += "教室";
            }
            if (_swapTeacher)
            {
                if (!string.IsNullOrEmpty(swapItems)) swapItems += "和";
                swapItems += "教师";
            }

            return $"交换课程分配 #{_assignmentId1} 和 #{_assignmentId2} 的{swapItems}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignmentId1, _assignmentId2 };
        }
    }
}