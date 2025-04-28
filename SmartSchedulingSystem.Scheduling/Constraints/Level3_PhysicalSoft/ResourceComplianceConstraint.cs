using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level3_PhysicalSoft
{
    /// <summary>
    /// Resource compliance constraint - Combines physical resource matching constraints
    /// </summary>
    public class ResourceComplianceConstraint : BaseConstraint, IConstraint
    {
        /// <summary>
        /// Feature switches
        /// </summary>
        private readonly bool _enableClassroomTypeMatch;
        private readonly bool _enableEquipmentRequirement;

        /// <summary>
        /// Constraint definition ID
        /// </summary>
        public override string DefinitionId => ConstraintDefinitions.ClassroomTypeMatch;

        /// <summary>
        /// Basic rule
        /// </summary>
        public override string BasicRule => BasicSchedulingRules.ResourcePreference;
        
        /// <summary>
        /// ID
        /// </summary>
        public override int Id => 9;
        
        /// <summary>
        /// Name
        /// </summary>
        public override string Name { get; } = "Resource Compliance Constraint";
        
        /// <summary>
        /// Description
        /// </summary>
        public override string Description { get; } = "Ensures courses are assigned to appropriate classrooms and equipment requirements are met";
        
        /// <summary>
        /// Whether this is a hard constraint
        /// </summary>
        public override bool IsHard => false;
        
        /// <summary>
        /// Constraint hierarchy level
        /// </summary>
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level3_PhysicalSoft;
        
        /// <summary>
        /// Constraint category
        /// </summary>
        public override string Category => "ResourceAllocation";

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceComplianceConstraint() : this(true, true)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceComplianceConstraint(
            bool enableClassroomTypeMatch,
            bool enableEquipmentRequirement)
        {
            Weight = 0.5;
            
            _enableClassroomTypeMatch = enableClassroomTypeMatch;
            _enableEquipmentRequirement = enableEquipmentRequirement;
        }

        /// <summary>
        /// Evaluate constraint
        /// </summary>
        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (!IsValidSolution(solution))
                return (1.0, new List<SchedulingConflict>());

            var subScores = new List<(double Score, double Weight)>();
            var allConflicts = new List<SchedulingConflict>();
            
            // Evaluate classroom type matching
            if (_enableClassroomTypeMatch)
            {
                var (score, conflicts) = EvaluateClassroomTypeMatch(solution);
                subScores.Add((score, 0.5));
                allConflicts.AddRange(conflicts);
            }
            
            // Evaluate equipment requirements
            if (_enableEquipmentRequirement)
            {
                var (score, conflicts) = EvaluateEquipmentRequirement(solution);
                subScores.Add((score, 0.5));
                allConflicts.AddRange(conflicts);
            }

            // Calculate weighted average score
            double finalScore = 1.0;
            if (subScores.Any())
            {
                double totalWeight = subScores.Sum(s => s.Weight);
                finalScore = subScores.Sum(s => s.Score * s.Weight) / totalWeight;
            }

            return (finalScore, allConflicts);
        }

        private bool IsValidSolution(SchedulingSolution solution)
        {
            return solution != null && 
                   solution.Assignments != null && 
                   solution.Assignments.Count > 0 && 
                   solution.Problem != null;
        }

        private (double Score, List<SchedulingConflict> Conflicts) EvaluateClassroomTypeMatch(SchedulingSolution solution)
        {
            List<SchedulingConflict> conflicts = new List<SchedulingConflict>();
            int validAssignments = 0;
            int mismatchCount = 0;
            
            foreach (var assignment in solution.Assignments)
            {
                if (assignment.ClassroomId <= 0)
                    continue;

                validAssignments++;
                
                // When using enhanced level, prioritize course resource requirement data
                bool useEnhancedData = solution.Problem.CourseResourceRequirements != null && 
                                     solution.Problem.CourseResourceRequirements.Any() &&
                                     solution.Problem.ClassroomResources != null && 
                                     solution.Problem.ClassroomResources.Any();
                
                if (useEnhancedData)
                {
                    var courseRequirement = solution.Problem.CourseResourceRequirements
                        .FirstOrDefault(r => r.CourseSectionId == assignment.CourseSectionId);
                    var classroomResource = solution.Problem.ClassroomResources
                        .FirstOrDefault(r => r.ClassroomId == assignment.ClassroomId);
                    
                    if (courseRequirement != null && classroomResource != null && 
                        courseRequirement.PreferredRoomTypes.Any())
                    {
                        bool matchFound = courseRequirement.PreferredRoomTypes.Contains(classroomResource.RoomType);
                        
                        if (!matchFound)
                        {
                            mismatchCount++;
                            conflicts.Add(CreateEnhancedClassroomTypeMismatchConflict(
                                solution, 
                                courseRequirement, 
                                classroomResource,
                                assignment));
                        }
                    }
                }
                else
                {
                    // Use traditional evaluation method
                    var course = solution.Problem.CourseSections.FirstOrDefault(c => c.Id == assignment.CourseSectionId);
                    var classroom = solution.Problem.Classrooms.FirstOrDefault(cr => cr.Id == assignment.ClassroomId);

                    if (course != null && classroom != null && 
                        !string.IsNullOrEmpty(course.RequiredClassroomType) && 
                        classroom.ClassroomType != course.RequiredClassroomType)
                    {
                        mismatchCount++;
                        conflicts.Add(CreateClassroomTypeMismatchConflict(solution, course, classroom));
                    }
                }
            }

            double score = validAssignments > 0 ? Math.Max(0, 1.0 - ((double)mismatchCount / validAssignments)) : 1.0;
            return (score, conflicts);
        }

        private SchedulingConflict CreateEnhancedClassroomTypeMismatchConflict(
            SchedulingSolution solution, 
            CourseResourceRequirement courseRequirement, 
            ClassroomResource classroomResource,
            SchedulingAssignment assignment)
        {
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.ClassroomTypeMismatch,
                Description = $"Course {courseRequirement.CourseName} requires classroom type(s) {string.Join(", ", courseRequirement.PreferredRoomTypes)}, " +
                             $"but was assigned to a classroom of type {classroomResource.RoomType}",
                Severity = ConflictSeverity.Minor,
                Category = "Classroom Type Mismatch",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Courses", new List<int> { assignment.CourseSectionId } },
                    { "Classrooms", new List<int> { assignment.ClassroomId } }
                }
            };
        }

        private SchedulingConflict CreateClassroomTypeMismatchConflict(SchedulingSolution solution, CourseSectionInfo course, ClassroomInfo classroom)
        {
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.ClassroomTypeMismatch,
                Description = $"Course {course.CourseName} requires classroom type {course.RequiredClassroomType}, but was assigned to a classroom of type {classroom.Type}",
                Severity = ConflictSeverity.Minor,
                Category = "Classroom Type Mismatch",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Courses", new List<int> { course.Id } },
                    { "Classrooms", new List<int> { classroom.Id } }
                }
            };
        }

        private (double Score, List<SchedulingConflict> Conflicts) EvaluateEquipmentRequirement(SchedulingSolution solution)
        {
            List<SchedulingConflict> conflicts = new List<SchedulingConflict>();
            int totalWithEquipmentRequirements = 0;
            int mismatchCount = 0;
            
            foreach (var assignment in solution.Assignments)
            {
                if (assignment.ClassroomId <= 0)
                    continue;

                // When using enhanced level, prioritize course resource requirement data
                bool useEnhancedData = solution.Problem.CourseResourceRequirements != null && 
                                     solution.Problem.CourseResourceRequirements.Any() &&
                                     solution.Problem.ClassroomResources != null && 
                                     solution.Problem.ClassroomResources.Any();
                
                if (useEnhancedData)
                {
                    var courseRequirement = solution.Problem.CourseResourceRequirements
                        .FirstOrDefault(r => r.CourseSectionId == assignment.CourseSectionId);
                    var classroomResource = solution.Problem.ClassroomResources
                        .FirstOrDefault(r => r.ClassroomId == assignment.ClassroomId);
                    
                    if (courseRequirement != null && classroomResource != null && 
                        courseRequirement.ResourceTypes.Any())
                    {
                        totalWithEquipmentRequirements++;
                        var missingResources = courseRequirement.ResourceTypes
                            .Where(e => !classroomResource.ResourceTypes.Contains(e))
                            .ToList();
                            
                        if (missingResources.Any())
                        {
                            mismatchCount++;
                            conflicts.Add(CreateEnhancedEquipmentMismatchConflict(
                                solution, 
                                courseRequirement, 
                                classroomResource,
                                assignment,
                                missingResources));
                        }
                    }
                }
                else
                {
                    // Use traditional evaluation method
                    var course = solution.Problem.CourseSections.FirstOrDefault(c => c.Id == assignment.CourseSectionId);
                    var classroom = solution.Problem.Classrooms.FirstOrDefault(cr => cr.Id == assignment.ClassroomId);

                    if (course != null && classroom != null && 
                        !string.IsNullOrEmpty(course.RequiredEquipment))
                    {
                        totalWithEquipmentRequirements++;
                        var courseEquipmentList = course.RequiredEquipment.Split(',').Select(e => e.Trim()).ToList();
                        var classroomEquipmentList = classroom.Equipment.Split(',').Select(e => e.Trim()).ToList();
                        
                        var missingEquipment = courseEquipmentList
                            .Where(e => !classroomEquipmentList.Contains(e))
                            .ToList();
                            
                        if (missingEquipment.Any())
                        {
                            mismatchCount++;
                            conflicts.Add(CreateEquipmentMismatchConflict(solution, course, classroom, missingEquipment));
                        }
                    }
                }
            }

            double score = totalWithEquipmentRequirements > 0 ? 
                Math.Max(0, 1.0 - ((double)mismatchCount / totalWithEquipmentRequirements)) : 1.0;
            return (score, conflicts);
        }

        private SchedulingConflict CreateEnhancedEquipmentMismatchConflict(
            SchedulingSolution solution, 
            CourseResourceRequirement courseRequirement, 
            ClassroomResource classroomResource,
            SchedulingAssignment assignment,
            List<string> missingResources)
        {
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.EquipmentMismatch,
                Description = $"Course {courseRequirement.CourseName} requires equipment: {string.Join(", ", missingResources)}, " +
                             $"which are not available in the assigned classroom",
                Severity = ConflictSeverity.Minor,
                Category = "Equipment Requirement Mismatch",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Courses", new List<int> { assignment.CourseSectionId } },
                    { "Classrooms", new List<int> { assignment.ClassroomId } }
                }
            };
        }

        private SchedulingConflict CreateEquipmentMismatchConflict(
            SchedulingSolution solution, CourseSectionInfo course, ClassroomInfo classroom, List<string> missingEquipment)
        {
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.EquipmentMismatch,
                Description = $"Course {course.CourseName} requires equipment: {string.Join(", ", missingEquipment)}, " +
                             $"which are not available in the assigned classroom",
                Severity = ConflictSeverity.Minor,
                Category = "Equipment Requirement Mismatch",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Courses", new List<int> { course.Id } },
                    { "Classrooms", new List<int> { classroom.Id } }
                }
            };
        }
    }
} 