// Equipment requirement constraint - Soft constraint
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level3_PhysicalSoft
{
    public class EquipmentRequirementConstraint : IConstraint
    {
        private readonly Dictionary<int, List<string>> _sectionRequiredEquipment; // Section ID -> List of required equipment
        private readonly Dictionary<int, List<string>> _classroomEquipment; // Classroom ID -> List of available equipment

        public int Id { get; } = 8;
        public string Name { get; } = "Equipment Requirements";
        public string Description { get; } = "Ensures classrooms have the required equipment for courses";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.8;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level3_PhysicalSoft;
        public string Category => "Physical Resources";
        
        // Add missing properties
        public string DefinitionId => "EquipmentRequirementConstraint";
        public string BasicRule => "ResourceRequirements";

        public EquipmentRequirementConstraint(
            Dictionary<int, List<string>> sectionRequiredEquipment,
            Dictionary<int, List<string>> classroomEquipment)
        {
            _sectionRequiredEquipment = sectionRequiredEquipment ?? throw new ArgumentNullException(nameof(sectionRequiredEquipment));
            _classroomEquipment = classroomEquipment ?? throw new ArgumentNullException(nameof(classroomEquipment));
        }

        public EquipmentRequirementConstraint()
        {
            _sectionRequiredEquipment = new Dictionary<int, List<string>>();
            _classroomEquipment = new Dictionary<int, List<string>>();
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            int totalRequirements = 0;
            int satisfiedRequirements = 0;

            foreach (var assignment in solution.Assignments)
            {
                // Check if course has equipment requirements
                if (_sectionRequiredEquipment.TryGetValue(assignment.SectionId, out List<string> requiredEquipment) &&
                    requiredEquipment.Count > 0)
                {
                    totalRequirements++;

                    // Get equipment provided by the classroom
                    bool hasEquipment = _classroomEquipment.TryGetValue(assignment.ClassroomId, out List<string> availableEquipment);

                    if (hasEquipment)
                    {
                        // Check if all required equipment is available
                        bool allRequirementsMet = requiredEquipment.All(req => availableEquipment.Contains(req));

                        if (allRequirementsMet)
                        {
                            satisfiedRequirements++;
                        }
                        else
                        {
                            // Find missing equipment
                            var missingEquipment = requiredEquipment.Where(req => !availableEquipment.Contains(req)).ToList();

                            conflicts.Add(new SchedulingConflict
                            {
                                ConstraintId = Id,
                                Type = SchedulingConflictType.ClassroomTypeMismatch, // Using type mismatch type, can define new type if needed
                                Description = $"Missing equipment for course {assignment.SectionCode} in classroom {assignment.ClassroomName}: " +
                                             $"{string.Join(", ", missingEquipment)}",
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
                        // No classroom equipment information, add conflict
                        conflicts.Add(new SchedulingConflict
                        {
                            ConstraintId = Id,
                            Type = SchedulingConflictType.ClassroomTypeMismatch,
                            Description = $"No equipment information available for classroom {assignment.ClassroomName} " +
                                         $"required by course {assignment.SectionCode}",
                            Severity = ConflictSeverity.Minor,
                            InvolvedEntities = new Dictionary<string, List<int>>
                            {
                                { "Sections", new List<int> { assignment.SectionId } },
                                { "Classrooms", new List<int> { assignment.ClassroomId } }
                            }
                        });
                    }
                }
            }

            // Calculate satisfaction rate as score
            double score = totalRequirements > 0 ? (double)satisfiedRequirements / totalRequirements : 1.0;

            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
} 