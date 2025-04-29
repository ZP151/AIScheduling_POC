using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level1_CoreHard
{
    /// <summary>
    /// Teacher conflict constraint: Ensures a teacher can only teach one course at a time
    /// Core hard constraint - Level1_CoreHard
    /// </summary>
    public class TeacherConflictConstraint : BaseConstraint
    {
        public override int Id => 1;
        public override string Name => "Teacher Conflict Avoidance";
        public override string Description => "Ensures a teacher is not assigned to multiple courses in the same time slot";
        public override bool IsHard => true;
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_CoreHard;
        public override string Category => "Resource Conflicts";
        public override string DefinitionId => ConstraintDefinitions.TeacherConflict;
        public override string BasicRule => BasicSchedulingRules.ResourceConflictAvoidance;

        public TeacherConflictConstraint()
        {
            IsActive = true;
            Weight = 1.0;
        }

        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            
            // Detect teacher conflicts
            var teacherTimeSlots = new Dictionary<(int teacherId, int timeSlotId), List<SchedulingAssignment>>();
            
            foreach (var assignment in solution.Assignments)
            {
                var key = (assignment.TeacherId, assignment.TimeSlotId);
                
                if (!teacherTimeSlots.ContainsKey(key))
                {
                    teacherTimeSlots[key] = new List<SchedulingAssignment>();
                }
                
                teacherTimeSlots[key].Add(assignment);
            }
            
            // Add all conflicts
            foreach (var item in teacherTimeSlots)
            {
                if (item.Value.Count > 1)
                {
                    var (teacherId, timeSlotId) = item.Key;
                    var conflictingSections = item.Value.Select(a => a.SectionId).ToList();
                    
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = Id,
                        Type = SchedulingConflictType.TeacherConflict,
                        Description = $"Teacher (ID: {teacherId}) is assigned to multiple courses at the same time slot",
                        Severity = ConflictSeverity.Critical,
                        InvolvedEntities = new Dictionary<string, List<int>>
                        {
                            { "Teachers", new List<int> { teacherId } },
                            { "Sections", conflictingSections }
                        },
                        InvolvedTimeSlots = new List<int> { timeSlotId }
                    });
                }
            }
            
            // Score is 1 if no conflicts, 0 otherwise (hard constraint)
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