using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level2_ConfigurableHard
{
    /// <summary>
    /// 教室可用性约束 - 教室只能在其可用时间段被使用
    /// </summary>
    public class ClassroomAvailabilityConstraint : BaseConstraint, IConstraint
    {
        /// <summary>
        /// 教室不可用时间字典(教室ID -> 不可用时间列表)
        /// </summary>
        private readonly Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>> _classroomUnavailableTimes;
        
        /// <summary>
        /// 不可用时间段列表
        /// </summary>
        protected readonly List<(DateTime Start, DateTime End, string Reason)> UnavailablePeriods;

        /// <summary>
        /// 学期日期字典 (问题ID -> 学期起止时间)
        /// </summary>
        protected readonly Dictionary<int, (DateTime Start, DateTime End)> SemesterDates;

        /// <summary>
        /// 约束定义ID
        /// </summary>
        public override string DefinitionId => ConstraintDefinitions.ClassroomAvailability;
        
        /// <summary>
        /// 约束ID
        /// </summary>
        public override int Id => 201;
        
        /// <summary>
        /// 约束名称
        /// </summary>
        public override string Name { get; } = "教室可用性约束";
        
        /// <summary>
        /// 约束描述
        /// </summary>
        public override string Description { get; } = "教室只能在其可用时间段被使用";
        
        /// <summary>
        /// 是否是硬约束
        /// </summary>
        public override bool IsHard => true;
        
        /// <summary>
        /// 约束层级
        /// </summary>
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level2_ConfigurableHard;
        
        /// <summary>
        /// 约束类别
        /// </summary>
        public override string Category => ConstraintCategory.TimeAllocation;
        
        /// <summary>
        /// 关联的基本排课规则
        /// </summary>
        public override string BasicRule => BasicSchedulingRules.ResourceAvailability;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ClassroomAvailabilityConstraint() : base()
        {
            _classroomUnavailableTimes = new Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>>();
            UnavailablePeriods = new List<(DateTime Start, DateTime End, string Reason)>();
            SemesterDates = new Dictionary<int, (DateTime Start, DateTime End)>();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="classroomUnavailableTimes">教室不可用时间字典</param>
        /// <param name="unavailablePeriods">全局不可用时间段(如节假日)</param>
        /// <param name="semesterDates">学期日期信息</param>
        public ClassroomAvailabilityConstraint(
            Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>> classroomUnavailableTimes,
            List<(DateTime Start, DateTime End, string Reason)> unavailablePeriods,
            Dictionary<int, (DateTime Start, DateTime End)> semesterDates) : base()
        {
            _classroomUnavailableTimes = classroomUnavailableTimes ?? new Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>>();
            UnavailablePeriods = unavailablePeriods ?? new List<(DateTime Start, DateTime End, string Reason)>();
            SemesterDates = semesterDates ?? new Dictionary<int, (DateTime Start, DateTime End)>();
        }

        /// <summary>
        /// 添加教室不可用时间
        /// </summary>
        /// <param name="classroomId">教室ID</param>
        /// <param name="start">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <param name="reason">原因</param>
        public void AddClassroomUnavailableTime(int classroomId, DateTime start, DateTime end, string reason)
        {
            if (!_classroomUnavailableTimes.ContainsKey(classroomId))
            {
                _classroomUnavailableTimes[classroomId] = new List<(DateTime, DateTime, string)>();
            }
            _classroomUnavailableTimes[classroomId].Add((start, end, reason));
        }

        /// <summary>
        /// 评估约束
        /// </summary>
        /// <param name="solution">排课方案</param>
        /// <returns>约束评估结果</returns>
        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (!IsValidSolution(solution))
            {
                return (0, new List<SchedulingConflict>());
            }
            
            var conflicts = new List<SchedulingConflict>();
            
            // 遍历所有课程分配
            foreach (var assignment in solution.Assignments)
            {
                int classroomId = assignment.ClassroomId;
                int timeSlotId = assignment.TimeSlotId;
                
                // 从问题定义中查找教室可用性
                var availability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ca => ca.ClassroomId == classroomId && ca.TimeSlotId == timeSlotId);
                
                // 如果找到可用性记录且教室不可用
                if (availability != null && !availability.IsAvailable)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = Id,
                        Type = SchedulingConflictType.ClassroomUnavailable,
                        Description = $"Classroom {assignment.ClassroomName} is not available at time slot {timeSlotId}",
                        Severity = ConflictSeverity.Severe,
                        InvolvedEntities = new Dictionary<string, List<int>>
                        {
                            { "Classrooms", new List<int> { classroomId } },
                            { "TimeSlots", new List<int> { timeSlotId } }
                        }
                    });
                }
            }
            
            // 计算约束满足度分数
            double score = conflicts.Count == 0 ? 1.0 : 0.0;
            
            return (score, conflicts);
        }

        private bool IsValidSolution(SchedulingSolution solution)
        {
            return solution != null && 
                   solution.Assignments != null && 
                   solution.Assignments.Count > 0 && 
                   solution.Problem != null &&
                   solution.Problem.ClassroomAvailabilities != null &&
                   solution.Problem.ClassroomAvailabilities.Count > 0;
        }

        /// <summary>
        /// 检查是否满足约束
        /// </summary>
        /// <param name="solution">排课方案</param>
        /// <returns>是否满足</returns>
        public override bool IsSatisfied(SchedulingSolution solution)
        {
            return Evaluate(solution).Score >= 1.0;
        }

        /// <summary>
        /// 检查两个时间段是否重叠
        /// </summary>
        protected bool DoPeriodsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1 < end2 && start2 < end1;
        }

        /// <summary>
        /// 检查特定时间段是否不可用
        /// </summary>
        protected string CheckTimeAvailability(DateTime startTime, DateTime endTime)
        {
            foreach (var period in UnavailablePeriods)
            {
                if (DoPeriodsOverlap(startTime, endTime, period.Start, period.End))
                {
                    return period.Reason;
                }
            }
            return null;
        }

        /// <summary>
        /// 从学期信息和周次计算具体日期
        /// </summary>
        protected DateTime? CalculateDate(int problemId, int week, int dayOfWeek)
        {
            if (!SemesterDates.TryGetValue(problemId, out var semesterDates))
                return null;

            // 计算当前周的开始日期
            DateTime weekStartDate = semesterDates.Start.AddDays((week - 1) * 7);
            
            // 计算具体的日期（根据星期几）
            int dayOffset = dayOfWeek - 1; // 假设dayOfWeek从1开始，1=周一
            return weekStartDate.AddDays(dayOffset);
        }
    }
}