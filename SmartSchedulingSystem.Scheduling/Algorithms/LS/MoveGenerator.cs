using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Engine.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using Microsoft.Extensions.Logging;
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
        private readonly Random _random = new Random();
        private readonly ILogger<MoveGenerator> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly SchedulingParameters _parameters;

        public MoveGenerator(
            ILogger<MoveGenerator> logger,
            ConstraintManager constraintManager,
            SchedulingParameters parameters = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _parameters = parameters ?? new SchedulingParameters();
        }

        /// <summary>
        /// 生成有效的移动操作
        /// </summary>
        /// <param name="solution">当前解决方案</param>
        /// <param name="assignment">要优化的课程分配</param>
        /// <param name="maxMoves">最多生成的移动数量</param>
        /// <returns>有效移动列表</returns>
        public List<IMove> GenerateValidMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            int maxMoves = 10)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));

            try
            {
                _logger.LogDebug($"为分配 #{assignment.Id} 生成移动操作，最大数量: {maxMoves}");

                var validMoves = new List<IMove>();

                // 添加时间移动
                AddTimeSlotMoves(solution, assignment, validMoves);

                // 添加教室移动
                AddRoomMoves(solution, assignment, validMoves);

                // 添加教师移动
                AddTeacherMoves(solution, assignment, validMoves);

                // 添加交换移动
                AddSwapMoves(solution, assignment, validMoves);

                _logger.LogDebug($"生成了 {validMoves.Count} 个有效移动操作");

                // 如果生成的移动太多，随机选择maxMoves个
                if (validMoves.Count > maxMoves)
                {
                    validMoves = validMoves
                        .OrderBy(x => _random.Next())
                        .Take(maxMoves)
                        .ToList();

                    _logger.LogDebug($"随机选择了 {maxMoves} 个移动操作");
                }

                return validMoves;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"生成移动操作时出错: {ex.Message}");
                return new List<IMove>();
            }
        }

        /// <summary>
        /// 添加时间移动
        /// </summary>
        private void AddTimeSlotMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 获取所有可用的时间槽
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);

            _logger.LogDebug($"找到 {availableTimeSlots.Count} 个可用时间槽供移动");

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
        private void AddRoomMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 获取所有合适的教室
            var suitableRooms = GetSuitableRooms(solution, assignment);

            _logger.LogDebug($"找到 {suitableRooms.Count} 个合适的教室供移动");

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
        private void AddTeacherMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 获取所有有资格的教师
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);

            _logger.LogDebug($"找到 {qualifiedTeachers.Count} 个有资格的教师供移动");

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
        private void AddSwapMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 找出可能的交换对象
            var potentialSwapPartners = FindPotentialSwapPartners(solution, assignment);

            _logger.LogDebug($"找到 {potentialSwapPartners.Count} 个潜在交换伙伴");

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

                // 交换教师（在某些情况下可能需要）
                if (_parameters.AllowTeacherSwap)
                {
                    var teacherSwap = new SwapMove(assignment.Id, partnerId, false, false, true);
                    if (IsValidMove(solution, teacherSwap))
                    {
                        moves.Add(teacherSwap);
                    }

                    // 全部交换（时间、教室、教师）
                    var completeSwap = new SwapMove(assignment.Id, partnerId, true, true, true);
                    if (IsValidMove(solution, completeSwap))
                    {
                        moves.Add(completeSwap);
                    }
                }
            }
        }

        /// <summary>
        /// 获取可用的时间槽ID列表
        /// </summary>
        private List<int> GetAvailableTimeSlots(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            if (solution.Problem == null)
            {
                _logger.LogWarning("解决方案中缺少问题信息，无法获取可用时间槽");
                return new List<int>();
            }

            try
            {
                // 所有时间槽ID
                var allTimeSlotIds = solution.Problem.TimeSlots.Select(t => t.Id).ToList();

                // 要排除的当前时间槽
                allTimeSlotIds.Remove(assignment.TimeSlotId);

                // 排除不可用的时间槽
                var teacherUnavailableTimeSlots = solution.Problem.TeacherAvailabilities
                    .Where(ta => ta.TeacherId == assignment.TeacherId && !ta.IsAvailable)
                    .Select(ta => ta.TimeSlotId)
                    .ToList();

                // 移除教师不可用的时间槽
                allTimeSlotIds = allTimeSlotIds
                    .Where(id => !teacherUnavailableTimeSlots.Contains(id))
                    .ToList();

                return allTimeSlotIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用时间槽时出错");
                return new List<int>();
            }
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