using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level1_CoreHard
{
    /// <summary>
    /// Classroom capacity constraint: Ensures classroom capacity meets course enrollment requirements
    /// Core hard constraint - Level1_CoreHard
    /// </summary>
    public class ClassroomCapacityConstraint : BaseConstraint
    {
        private readonly Dictionary<int, int> _classroomCapacities;
        private readonly Dictionary<int, int> _expectedEnrollments;

        public override int Id => 3;
        public override string Name => "Classroom Capacity";
        public override string Description => "Ensures classroom capacity is sufficient for course enrollment";
        public override bool IsHard => true;
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_CoreHard;
        public override string Category => "Resource Capacity";
        public override string DefinitionId => ConstraintDefinitions.ClassroomCapacity;
        public override string BasicRule => BasicSchedulingRules.ResourceCapacityRespect;

        public ClassroomCapacityConstraint()
        {
            IsActive = true;
            Weight = 1.0;
            _classroomCapacities = new Dictionary<int, int>();
            _expectedEnrollments = new Dictionary<int, int>();
        }

        public ClassroomCapacityConstraint(
            Dictionary<int, int> classroomCapacities,
            Dictionary<int, int> expectedEnrollments)
        {
            IsActive = true;
            Weight = 1.0;
            _classroomCapacities = classroomCapacities ?? throw new ArgumentNullException(nameof(classroomCapacities));
            _expectedEnrollments = expectedEnrollments ?? throw new ArgumentNullException(nameof(expectedEnrollments));
        }

        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            
            // Fill capacity dictionary (if empty)
            if (_classroomCapacities.Count == 0 && solution.Problem?.Classrooms != null)
            {
                foreach (var classroom in solution.Problem.Classrooms)
                {
                    _classroomCapacities[classroom.Id] = classroom.Capacity;
                }
            }
            
            // Fill enrollment dictionary (if empty)
            if (_expectedEnrollments.Count == 0 && solution.Problem?.CourseSections != null)
            {
                foreach (var section in solution.Problem.CourseSections)
                {
                    _expectedEnrollments[section.Id] = section.Enrollment;
                }
            }

            // Check if each assigned classroom's capacity meets course requirements
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
                            Description = $"Classroom (ID: {assignment.ClassroomId}) has capacity {capacity} but course (ID: {assignment.SectionId}) has enrollment {enrollment}",
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

            // Hard constraint: Score is 1 if no conflicts, 0 otherwise
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