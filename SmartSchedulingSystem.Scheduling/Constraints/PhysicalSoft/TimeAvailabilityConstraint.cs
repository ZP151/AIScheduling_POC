// 6. 时间可用性约束 - 软约束
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Soft
{
    public class TimeAvailabilityConstraint : IConstraint
    {
        private readonly List<(DateTime Start, DateTime End, string Reason)> _unavailablePeriods;
        private readonly Dictionary<int, (DateTime Start, DateTime End)> _semesterDates;

        public int Id { get; } = 9;
        public string Name { get; } = "Time Availability";
        public string Description { get; } = "Ensures courses are not scheduled during holidays or special events";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.9;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level2_PhysicalSoft;
        public string Category => "Time Resources";

        public TimeAvailabilityConstraint(
            List<(DateTime Start, DateTime End, string Reason)> unavailablePeriods,
            Dictionary<int, (DateTime Start, DateTime End)> semesterDates)
        {
            _unavailablePeriods = unavailablePeriods ?? throw new ArgumentNullException(nameof(unavailablePeriods));
            _semesterDates = semesterDates ?? throw new ArgumentNullException(nameof(semesterDates));
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();

            // 获取学期信息
            if (!_semesterDates.TryGetValue(solution.ProblemId, out var semesterDates))
            {
                // 没有学期日期信息，无法评估
                return (1.0, conflicts);
            }

            // 计算学期的周数
            TimeSpan semesterDuration = semesterDates.End - semesterDates.Start;
            int semesterWeeks = (int)Math.Ceiling(semesterDuration.TotalDays / 7);

            foreach (var assignment in solution.Assignments)
            {
                // 对每个周模式检查
                foreach (int week in assignment.WeekPattern)
                {
                    if (week > semesterWeeks)
                        continue; // 超出学期范围

                    // 计算当前周的日期
                    DateTime weekStartDate = semesterDates.Start.AddDays((week - 1) * 7);

                    // 计算具体的课程日期（根据星期几）
                    int dayOffset = assignment.DayOfWeek - 1; // 假设DayOfWeek从1开始，1=周一
                    DateTime classDate = weekStartDate.AddDays(dayOffset);

                    // 创建课程的时间范围
                    DateTime classStartTime = classDate.Date.Add(assignment.StartTime);
                    DateTime classEndTime = classDate.Date.Add(assignment.EndTime);

                    // 检查是否与不可用时间重叠
                    foreach (var period in _unavailablePeriods)
                    {
                        if (DoPeriodsOverlap(classStartTime, classEndTime, period.Start, period.End))
                        {
                            conflicts.Add(new SchedulingConflict
                            {
                                ConstraintId = Id,
                                Type = SchedulingConflictType.HolidayConflict,
                                Description = $"Course {assignment.SectionCode} is scheduled during {period.Reason}: " +
                                             $"Week {week}, {classStartTime:yyyy-MM-dd HH:mm} to {classEndTime:yyyy-MM-dd HH:mm}",
                                Severity = ConflictSeverity.Moderate,
                                InvolvedEntities = new Dictionary<string, List<int>>
                                {
                                    { "Sections", new List<int> { assignment.SectionId } }
                                },
                                InvolvedTimeSlots = new List<int> { assignment.TimeSlotId }
                            });

                            // 一个时间段只记录一次冲突
                            break;
                        }
                    }
                }
            }

            // 计算得分：每个冲突减少10%的分数，最低为0
            double score = Math.Max(0, 1.0 - (conflicts.Count * 0.1));

            return (score, conflicts);
        }

        private bool DoPeriodsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1 < end2 && start2 < end1;
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}