using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Constraints.Hard
{
    public class ClassroomCapacityConstraint : IConstraint
    {
        private readonly Dictionary<int, int> _classroomCapacities;
        private readonly Dictionary<int, int> _expectedEnrollments;

        public int Id { get; } = 6;
        public string Name { get; } = "Classroom Capacity (Hard)";
        public string Description { get; } = "Ensures classroom capacity is sufficient for course enrollment";
        public bool IsHard { get; } = true;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_Hard;
        public string Category => "Physical Resources";
        
        public ClassroomCapacityConstraint(
            Dictionary<int, int> classroomCapacities,
            Dictionary<int, int> expectedEnrollments)
        {
            _classroomCapacities = classroomCapacities ?? throw new ArgumentNullException(nameof(classroomCapacities));
            _expectedEnrollments = expectedEnrollments ?? throw new ArgumentNullException(nameof(expectedEnrollments));
        }

        public ClassroomCapacityConstraint()
        {
            _classroomCapacities = new Dictionary<int, int>();
            _expectedEnrollments = new Dictionary<int, int>();
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();

            foreach (var assignment in solution.Assignments)
            {
                if (_classroomCapacities.TryGetValue(assignment.ClassroomId, out int capacity) &&
                    _expectedEnrollments.TryGetValue(assignment.SectionId, out int enrollment))
                {
                    if (capacity < enrollment)
                    {
                        conflicts.Add(new SchedulingConflict
                        {
                            ConstraintId = Id,
                            Type = SchedulingConflictType.ClassroomCapacityExceeded,
                            Description = $"Classroom {assignment.ClassroomName} has capacity {capacity} but course {assignment.SectionCode} has enrollment {enrollment}",
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
            }

            // 硬约束：如果没有冲突，得分为1，否则为0
            double score = conflicts.Count == 0 ? 1.0 : 0.0;

            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            // 直接检查是否有教室容量不足的情况
            foreach (var assignment in solution.Assignments)
            {
                if (_classroomCapacities.TryGetValue(assignment.ClassroomId, out int capacity) &&
                    _expectedEnrollments.TryGetValue(assignment.SectionId, out int enrollment))
                {
                    if (capacity < enrollment)
                    {
                        return false; // 找到一个容量不足的教室，约束不满足
                    }
                }
            }

            return true; // 所有教室容量都足够
        }
    }
}