using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Constraints.Soft
{
    public class ClassroomCapacityConstraint : IConstraint
    {
        private readonly Dictionary<int, int> _classroomCapacities;
        private readonly Dictionary<int, int> _expectedCapacities;

        public int Id { get; } = 3;
        public string Name { get; } = "Classroom Capacity";
        public string Description { get; } = "Classrooms should have enough capacity for course expectedCapacities";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.8;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level2_PhysicalSoft;
        public string Category => "Physical Resources";
        public ClassroomCapacityConstraint(
            Dictionary<int, int> classroomCapacities,
            Dictionary<int, int> expectedCapacities)
        {
            _classroomCapacities = classroomCapacities ?? throw new ArgumentNullException(nameof(classroomCapacities));
            _expectedCapacities = expectedCapacities ?? throw new ArgumentNullException(nameof(expectedCapacities));
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            double totalScore = 0;
            int evaluated = 0;

            foreach (var assignment in solution.Assignments)
            {
                if (_classroomCapacities.TryGetValue(assignment.ClassroomId, out int capacity) &&
                    _expectedCapacities.TryGetValue(assignment.SectionId, out int enrollment))
                {
                    evaluated++;

                    // 计算容量评分（1表示教室容量远超期望人数，0表示教室容量不足）
                    double capacityRatio = enrollment > 0 ? (double)capacity / enrollment : 1.0;
                    double assignmentScore;

                    if (capacityRatio >= 1.0)
                    {
                        // 教室容量足够
                        // 但如果教室过大，也会有效率问题（0.95 - 1.05是最佳区间）
                        assignmentScore = capacityRatio <= 1.05 ? 1.0 : 1.0 - Math.Min(0.5, (capacityRatio - 1.05) / 2);
                    }
                    else
                    {
                        // 教室容量不足
                        assignmentScore = capacityRatio;

                        // 如果容量不足，添加冲突
                        if (capacityRatio < 0.9)
                        {
                            conflicts.Add(new SchedulingConflict
                            {
                                ConstraintId = Id,
                                Type = SchedulingConflictType.ClassroomCapacityExceeded,
                                Description = $"Classroom {assignment.ClassroomName} has capacity {capacity} but course {assignment.SectionCode} has enrollment {enrollment}",
                                Severity = capacityRatio < 0.7 ? ConflictSeverity.Severe : ConflictSeverity.Moderate,
                                InvolvedEntities = new Dictionary<string, List<int>>
                                {
                                    { "Classrooms", new List<int> { assignment.ClassroomId } },
                                    { "Sections", new List<int> { assignment.SectionId } }
                                }
                            });
                        }
                    }

                    totalScore += assignmentScore;
                }
            }

            // 计算平均分
            double averageScore = evaluated > 0 ? totalScore / evaluated : 1.0;

            return (averageScore, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}