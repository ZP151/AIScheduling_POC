// 3. 教师可用性约束 - 硬约束
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Hard
{
    public class TeacherAvailabilityConstraint : IConstraint
    {
        private readonly Dictionary<(int TeacherId, int TimeSlotId), bool> _teacherAvailability;

        public int Id { get; } = 3;
        public string Name { get; } = "Teacher Availability";
        public string Description { get; } = "Ensures teachers are available during scheduled times";
        public bool IsHard { get; } = true;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_Hard;
        public string Category => "Resource Availability";

        public TeacherAvailabilityConstraint(Dictionary<(int TeacherId, int TimeSlotId), bool> teacherAvailability)
        {
            _teacherAvailability = teacherAvailability ?? throw new ArgumentNullException(nameof(teacherAvailability));
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();

            foreach (var assignment in solution.Assignments)
            {
                var key = (assignment.TeacherId, assignment.TimeSlotId);

                // 如果有记录教师在此时间段不可用，则添加冲突
                if (_teacherAvailability.TryGetValue(key, out bool isAvailable) && !isAvailable)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = Id,
                        Type = SchedulingConflictType.TeacherAvailabilityConflict,
                        Description = $"Teacher {assignment.TeacherName} is not available for course {assignment.SectionCode} " +
                                     $"at time slot {assignment.DayOfWeek}-{assignment.StartTime}-{assignment.EndTime}",
                        Severity = ConflictSeverity.Critical,
                        InvolvedEntities = new Dictionary<string, List<int>>
                        {
                            { "Teachers", new List<int> { assignment.TeacherId } },
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
            throw new NotImplementedException();
        }
    }
}
