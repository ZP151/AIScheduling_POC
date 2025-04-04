using SmartSchedulingSystem.Scheduling.Engine.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Engine.LS
{
    /// <summary>
    /// 负责在局部搜索阶段生成有效的优化移动
    /// </summary>
    public class MoveGenerator
    {
        private readonly ConstraintManager _constraintManager;
        private readonly Random _random = new Random();

        public MoveGenerator(ConstraintManager constraintManager)
        {
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
        }

        /// <summary>
        /// 生成有效的移动操作
        /// </summary>
        /// <param name="solution">当前解决方案</param>
        /// <param name="assignment">要优化的课程分配</param>
        /// <param name="maxMoves">最多生成的移动数量</param>
        /// <returns>有效移动列表</returns>
        public List<IMove> GenerateValidMoves(SchedulingSolution solution, SchedulingAssignment assignment, int maxMoves = 10)
        {
            var validMoves = new List<IMove>();

            // 添加时间移动
            AddTimeSlotMoves(solution, assignment, validMoves);

            // 添加教室移动
            AddRoomMoves(solution, assignment, validMoves);

            // 添加教师移动
            AddTeacherMoves(solution, assignment, validMoves);

            // 添加交换移动
            AddSwapMoves(solution, assignment, validMoves);

            // 如果生成的移动太多，随机选择maxMoves个
            if (validMoves.Count > maxMoves)
            {
                validMoves = validMoves
                    .OrderBy(x => _random.Next())
                    .Take(maxMoves)
                    .ToList();
            }

            return validMoves;
        }

        /// <summary>
        /// 添加时间移动
        /// </summary>
        private void AddTimeSlotMoves(SchedulingSolution solution, SchedulingAssignment assignment, List<IMove> moves)
        {
            // 获取所有可用的时间槽
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);

            foreach (var timeSlotId in availableTimeSlots)
            {
                var move = new TimeMove(assignment.Id, timeSlotId);

                // 验证移动是否满足所有硬约束
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }

        /// <summary>
        /// 添加教室移动
        /// </summary>
        private void AddRoomMoves(SchedulingSolution solution, SchedulingAssignment assignment, List<IMove> moves)
        {
            // 获取所有合适的教室
            var suitableRooms = GetSuitableRooms(solution, assignment);

            foreach (var roomId in suitableRooms)
            {
                var move = new RoomMove(assignment.Id, roomId);

                // 验证移动是否满足所有硬约束
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }

        /// <summary>
        /// 添加教师移动
        /// </summary>
        private void AddTeacherMoves(SchedulingSolution solution, SchedulingAssignment assignment, List<IMove> moves)
        {
            // 获取所有有资格的教师
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);

            foreach (var teacherId in qualifiedTeachers)
            {
                var move = new TeacherMove(assignment.Id, teacherId);

                // 验证移动是否满足所有硬约束
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }

        /// <summary>
        /// 添加交换移动
        /// </summary>
        private void AddSwapMoves(SchedulingSolution solution, SchedulingAssignment assignment, List<IMove> moves)
        {
            // 找出可能的交换对象
            var potentialSwapPartners = FindPotentialSwapPartners(solution, assignment);

            foreach (var partnerId in potentialSwapPartners)
            {
                // 时间交换
                var timeSwap = new SwapMove(assignment.Id, partnerId, true, false, false);
                if (IsValidMove(solution, timeSwap))
                {
                    moves.Add(timeSwap);
                }

                // 教室交换
                var roomSwap = new SwapMove(assignment.Id, partnerId, false, true, false);
                if (IsValidMove(solution, roomSwap))
                {
                    moves.Add(roomSwap);
                }

                // 时间和教室都交换
                var fullSwap = new SwapMove(assignment.Id, partnerId, true, true, false);
                if (IsValidMove(solution, fullSwap))
                {
                    moves.Add(fullSwap);
                }
            }
        }

        /// <summary>
        /// 获取可用的时间槽ID列表
        /// </summary>
        private List<int> GetAvailableTimeSlots(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 简单实现：返回所有与当前不同的时间槽
            // 实际实现中应考虑教师可用性等约束
            return solution.Problem.TimeSlots
                .Select(t => t.Id)
                .Where(id => id != assignment.TimeSlotId)
                .ToList();
        }

        /// <summary>
        /// 获取合适的教室ID列表
        /// </summary>
        private List<int> GetSuitableRooms(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 简单实现：返回所有与当前不同的教室
            // 实际实现中应考虑教室容量、设备要求等约束
            return solution.Problem.Classrooms
                .Select(r => r.Id)
                .Where(id => id != assignment.ClassroomId)
                .ToList();
        }

        /// <summary>
        /// 获取有资格的教师ID列表
        /// </summary>
        private List<int> GetQualifiedTeachers(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 简单实现：返回所有与当前不同的教师
            // 实际实现中应考虑教师学科专业、偏好等因素
            return solution.Problem.Teachers
                .Select(t => t.Id)
                .Where(id => id != assignment.TeacherId)
                .ToList();
        }

        /// <summary>
        /// 查找潜在的交换伙伴ID列表
        /// </summary>
        private List<int> FindPotentialSwapPartners(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 简单实现：随机选择几个其他分配作为潜在交换伙伴
            return solution.Assignments
                .Where(a => a.Id != assignment.Id)
                .OrderBy(x => _random.Next())
                .Take(3)
                .Select(a => a.Id)
                .ToList();
        }

        /// <summary>
        /// 验证移动是否满足所有硬约束
        /// </summary>
        private bool IsValidMove(SchedulingSolution solution, IMove move)
        {
            // 应用移动到临时解
            var tempSolution = move.Apply(solution);

            // 验证所有硬约束
            foreach (var constraint in _constraintManager.GetHardConstraints())
            {
                if (!constraint.IsSatisfied(tempSolution))
                {
                    return false;
                }
            }

            return true;
        }
    }
}