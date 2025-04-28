// Classroom type matching constraint - Soft constraint
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level3_PhysicalSoft
{
    public class ClassroomTypeMatchConstraint : IConstraint
    {
        private readonly Dictionary<int, string> _courseSectionTypes; // Section ID -> Course type
        private readonly Dictionary<int, string> _classroomTypes; // Classroom ID -> Classroom type

        public int Id { get; } = 7;
        public string Name { get; } = "Classroom Type Match";
        public string Description { get; } = "Ensures courses are scheduled in appropriate type of classrooms";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.7;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level3_PhysicalSoft;
        public string Category => "Physical Resources";
        
        // Add missing properties
        public string DefinitionId => "ClassroomTypeMatchConstraint";
        public string BasicRule => "ResourceMatching";

        public ClassroomTypeMatchConstraint(
            Dictionary<int, string> courseSectionTypes,
            Dictionary<int, string> classroomTypes)
        {
            _courseSectionTypes = courseSectionTypes ?? throw new ArgumentNullException(nameof(courseSectionTypes));
            _classroomTypes = classroomTypes ?? throw new ArgumentNullException(nameof(classroomTypes));
        }

        public ClassroomTypeMatchConstraint()
        {
            _courseSectionTypes = new Dictionary<int, string>();
            _classroomTypes = new Dictionary<int, string>();

        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            int totalAssignments = 0;
            int matchingAssignments = 0;

            foreach (var assignment in solution.Assignments)
            {
                totalAssignments++;

                // Get course type and classroom type
                bool hasCourseType = _courseSectionTypes.TryGetValue(assignment.SectionId, out string courseType);
                bool hasRoomType = _classroomTypes.TryGetValue(assignment.ClassroomId, out string classroomType);

                if (hasCourseType && hasRoomType)
                {
                    bool isMatching = IsTypeMatching(courseType, classroomType);

                    if (isMatching)
                    {
                        matchingAssignments++;
                    }
                    else
                    {
                        // Not matching, add conflict
                        conflicts.Add(new SchedulingConflict
                        {
                            ConstraintId = Id,
                            Type = SchedulingConflictType.ClassroomTypeMismatch,
                            Description = $"Classroom type mismatch: Course {assignment.SectionCode} of type '{courseType}' " +
                                         $"is scheduled in classroom {assignment.ClassroomName} of type '{classroomType}'",
                            Severity = ConflictSeverity.Moderate,
                            InvolvedEntities = new Dictionary<string, List<int>>
                            {
                                { "Sections", new List<int> { assignment.SectionId } },
                                { "Classrooms", new List<int> { assignment.ClassroomId } }
                            }
                        });
                    }
                }
                else
                {
                    // If no type information available, assume matching (avoid over-penalization)
                    matchingAssignments++;
                }
            }

            // Calculate matching rate as score
            double score = totalAssignments > 0 ? (double)matchingAssignments / totalAssignments : 1.0;

            return (score, conflicts);
        }

        private bool IsTypeMatching(string courseType, string classroomType)
        {
            // Lab courses must be in lab rooms
            if (courseType.Contains("Lab") && !classroomType.Contains("Lab"))
                return false;

            // Computer courses must be in computer rooms
            if (courseType.Contains("Computer") && !classroomType.Contains("Computer"))
                return false;

            // Large courses need large classrooms
            if (courseType.Contains("Large") && !classroomType.Contains("Large"))
                return false;

            // Discussion groups need discussion rooms
            if (courseType.Contains("Discussion") && !classroomType.Contains("Discussion"))
                return false;

            // Regular courses can be in regular classrooms or better
            if (courseType.Contains("Regular"))
                return true;

            // Default to matching
            return true;
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
} 