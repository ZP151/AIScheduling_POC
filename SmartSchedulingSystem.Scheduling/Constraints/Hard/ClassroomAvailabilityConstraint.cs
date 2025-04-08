using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Constraints.Hard
{
    public class ClassroomAvailabilityConstraint : IConstraint
    {
        private readonly Dictionary<(int ClassroomId, int TimeSlotId), bool> _classroomAvailability;

        public int Id { get; } = 7;
        public string Name { get; } = "Classroom Availability";
        public string Description { get; } = "Ensures classrooms are available during scheduled times";
        public bool IsHard { get; } = true;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_Hard;
        public string Category => "Resource Availability";
        public ClassroomAvailabilityConstraint()
        {
            // 默认构造函数
            _classroomAvailability = new Dictionary<(int, int), bool>();
        }
        public ClassroomAvailabilityConstraint(Dictionary<(int ClassroomId, int TimeSlotId), bool> classroomAvailability)
        {
            _classroomAvailability = classroomAvailability ?? throw new ArgumentNullException(nameof(classroomAvailability));
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();

            foreach (var assignment in solution.Assignments)
            {
                var key = (assignment.ClassroomId, assignment.TimeSlotId);

                // 如果有记录教室在此时间段不可用，则添加冲突
                if (_classroomAvailability.TryGetValue(key, out bool isAvailable) && !isAvailable)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = Id,
                        Type = SchedulingConflictType.ClassroomAvailabilityConflict,
                        Description = $"Classroom {assignment.ClassroomName} is not available for course {assignment.SectionCode} " +
                                     $"at time slot {assignment.DayOfWeek}-{assignment.StartTime}-{assignment.EndTime}",
                        Severity = ConflictSeverity.Critical,
                        InvolvedEntities = new Dictionary<string, List<int>>
                        {
                            { "Classrooms", new List<int> { assignment.ClassroomId } },
                            { "Sections", new List<int> { assignment.SectionId } }
                        },
                        InvolvedTimeSlots = new List<int> { assignment.TimeSlotId }
                    });
                }
            }

            // 如果没有冲突，得分为1，否则为0（硬约束）
            double score = conflicts.Count == 0 ? 1.0 : 0.0;

            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            var (score, _) = Evaluate(solution);
            return score >= 1.0;
        }
    }
}