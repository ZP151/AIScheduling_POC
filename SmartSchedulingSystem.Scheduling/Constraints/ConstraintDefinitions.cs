using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    /// <summary>
    /// Constraint definitions class, centralized management of all scheduling constraints
    /// </summary>
    public static class ConstraintDefinitions
    {
        #region Basic scheduling hard constraints (Level1_CoreHard)

        /// <summary>
        /// Teacher conflict constraint (Level1_CoreHard)
        /// A teacher can only teach one course in the same time slot
        /// </summary>
        public const string TeacherConflict = "TeacherConflict";

        /// <summary>
        /// Classroom conflict constraint (Level1_CoreHard)
        /// A classroom can only host one course in the same time slot
        /// </summary>
        public const string ClassroomConflict = "ClassroomConflict";

        /// <summary>
        /// Student conflict constraint (Level1_CoreHard)
        /// A student cannot attend two courses at the same time
        /// </summary>
        public const string StudentConflict = "StudentConflict";

        /// <summary>
        /// Classroom capacity constraint (Level1_CoreHard)
        /// Classroom capacity must meet course demand (number of students)
        /// </summary>
        public const string ClassroomCapacity = "ClassroomCapacity";

        /// <summary>
        /// Course assignment constraint (Level1_CoreHard)
        /// Each course must be assigned to a time slot, classroom, and teacher
        /// </summary>
        public const string CourseAssignment = "CourseAssignment";

        /// <summary>
        /// Course section consistency constraint (Level1_CoreHard)
        /// Sections of the same course should be taught by the same teacher (unless specified otherwise)
        /// </summary>
        public const string CourseSectionConsistency = "CourseSectionConsistency";

        /// <summary>
        /// Prerequisite order constraint (Level1_CoreHard)
        /// Prerequisite courses must be completed before subsequent courses
        /// </summary>
        public const string PrerequisiteOrder = "PrerequisiteOrder";

        /// <summary>
        /// Time contiguity constraint (Level1_CoreHard)
        /// Multi-slot courses must be scheduled in contiguous time slots
        /// </summary>
        public const string TimeContiguity = "TimeContiguity";

        /// <summary>
        /// Mutual exclusion time constraint (Level1_CoreHard)
        /// Certain activities must be scheduled at different times
        /// </summary>
        public const string MutualExclusionTime = "MutualExclusionTime";

        /// <summary>
        /// Minimum time gap constraint (Level1_CoreHard)
        /// A minimum time gap must be maintained between certain activities
        /// </summary>
        public const string MinimumTimeGap = "MinimumTimeGap";

        /// <summary>
        /// Related courses constraint (Level1_CoreHard)
        /// Related courses (e.g., lecture and lab) must be scheduled according to specific rules
        /// </summary>
        public const string RelatedCourses = "RelatedCourses";

        #endregion

        #region Configurable hard constraints (Level2_ConfigurableHard)

        /// <summary>
        /// Teacher availability constraint (Level2_ConfigurableHard)
        /// Teachers can only teach during their available time slots
        /// </summary>
        public const string TeacherAvailability = "TeacherAvailability";

        /// <summary>
        /// Classroom availability constraint (Level2_ConfigurableHard)
        /// Classrooms can only be used during their available time slots
        /// </summary>
        public const string ClassroomAvailability = "ClassroomAvailability";

        /// <summary>
        /// Teacher maximum workload constraint (Level2_ConfigurableHard)
        /// Teachers' daily/weekly teaching hours must not exceed the maximum limit
        /// </summary>
        public const string TeacherMaxWorkload = "TeacherMaxWorkload";

        /// <summary>
        /// Teacher qualification constraint (Level2_ConfigurableHard)
        /// Teachers must be qualified to teach assigned courses
        /// </summary>
        public const string TeacherQualification = "TeacherQualification";

        /// <summary>
        /// Course time restriction constraint (Level2_ConfigurableHard)
        /// Certain courses can only be scheduled in specified time slots
        /// </summary>
        public const string CourseTimeRestriction = "CourseTimeRestriction";

        #endregion

        #region Physical soft constraints (Level3_PhysicalSoft)

        /// <summary>
        /// Classroom type match constraint (Level3_PhysicalSoft)
        /// Courses should be assigned to appropriate types of classrooms
        /// </summary>
        public const string ClassroomTypeMatch = "ClassroomTypeMatch";

        /// <summary>
        /// Equipment requirement constraint (Level3_PhysicalSoft)
        /// Classroom equipment must match course equipment requirements
        /// </summary>
        public const string EquipmentRequirement = "EquipmentRequirement";

        /// <summary>
        /// Time availability soft constraint (Level3_PhysicalSoft)
        /// Consider teachers' and students' preferred time slots
        /// </summary>
        public const string TimeAvailability = "TimeAvailability";

        /// <summary>
        /// Building capacity balance constraint (Level3_PhysicalSoft)
        /// Avoid scheduling too many courses in the same building at the same time
        /// </summary>
        public const string BuildingCapacityBalance = "BuildingCapacityBalance";

        /// <summary>
        /// Special facility utilization constraint (Level3_PhysicalSoft)
        /// Maximize utilization of special facilities (e.g., labs)
        /// </summary>
        public const string SpecialFacilityUtilization = "SpecialFacilityUtilization";

        #endregion

        #region Quality soft constraints (Level4_QualitySoft)

        /// <summary>
        /// Teacher preference constraint (Level4_QualitySoft)
        /// Assign teachers to their preferred courses whenever possible
        /// </summary>
        public const string TeacherPreference = "TeacherPreference";

        /// <summary>
        /// Teacher schedule compactness constraint (Level4_QualitySoft)
        /// Teachers' schedules should be compact, minimizing idle time
        /// </summary>
        public const string TeacherScheduleCompactness = "TeacherScheduleCompactness";

        /// <summary>
        /// Teacher workload constraint (Level4_QualitySoft)
        /// Teachers' workload should be balanced
        /// </summary>
        public const string TeacherWorkload = "TeacherWorkload";

        /// <summary>
        /// Student schedule quality constraint (Level4_QualitySoft)
        /// Student schedules should avoid long idle periods and too many back-to-back classes
        /// </summary>
        public const string StudentScheduleQuality = "StudentScheduleQuality";

        /// <summary>
        /// Course distribution constraint (Level4_QualitySoft)
        /// Courses should be evenly distributed throughout the week
        /// </summary>
        public const string CourseDistribution = "CourseDistribution";

        /// <summary>
        /// Teaching continuity constraint (Level4_QualitySoft)
        /// Related courses should be scheduled on adjacent days whenever possible
        /// </summary>
        public const string TeachingContinuity = "TeachingContinuity";

        /// <summary>
        /// Course priority constraint (Level4_QualitySoft)
        /// High-priority courses should receive better time slots and classroom resources
        /// </summary>
        public const string CoursePriority = "CoursePriority";

        /// <summary>
        /// Teacher mobility constraint (Level4_QualitySoft)
        /// Teachers should not need to quickly move between buildings for consecutive classes
        /// </summary>
        public const string TeacherMobility = "TeacherMobility";

        #endregion

        /// <summary>
        /// Get all basic scheduling hard constraints
        /// </summary>
        public static List<string> GetCoreHardConstraints()
        {
            return new List<string>
            {
                TeacherConflict,
                ClassroomConflict,
                StudentConflict,
                ClassroomCapacity,
                CourseAssignment,
                CourseSectionConsistency,
                TimeContiguity,
                MutualExclusionTime,
                MinimumTimeGap,
                RelatedCourses
            };
        }

        /// <summary>
        /// Get all configurable hard constraints
        /// </summary>
        public static List<string> GetConfigurableHardConstraints()
        {
            return new List<string>
            {
                TeacherAvailability,
                ClassroomAvailability,
                TeacherMaxWorkload,
                TeacherQualification,
                CourseTimeRestriction
            };
        }

        /// <summary>
        /// Get all physical soft constraints
        /// </summary>
        public static List<string> GetPhysicalSoftConstraints()
        {
            return new List<string>
            {
                ClassroomTypeMatch,
                EquipmentRequirement,
                TimeAvailability,
                BuildingCapacityBalance,
                SpecialFacilityUtilization
            };
        }

        /// <summary>
        /// Get all quality soft constraints
        /// </summary>
        public static List<string> GetQualitySoftConstraints()
        {
            return new List<string>
            {
                TeacherPreference,
                TeacherScheduleCompactness,
                TeacherWorkload,
                StudentScheduleQuality,
                CourseDistribution,
                TeachingContinuity,
                CoursePriority,
                TeacherMobility
            };
        }

        /// <summary>
        /// Get minimal essential constraints (for simplified mode or random solution generation)
        /// </summary>
        public static List<string> GetMinimalEssentialConstraints()
        {
            return new List<string>
            {
                TeacherConflict,
                ClassroomConflict,
                ClassroomCapacity,
                CourseAssignment
            };
        }

        /// <summary>
        /// Get constraints for university scheduling
        /// </summary>
        public static List<string> GetUniversityConstraints()
        {
            return new List<string>
            {
                // Core hard constraints
                TeacherConflict,
                ClassroomConflict,
                StudentConflict,
                ClassroomCapacity,
                CourseAssignment,
                PrerequisiteOrder,
                
                // Configurable hard constraints
                TeacherAvailability,
                ClassroomAvailability,
                TeacherQualification,
                
                // Important soft constraints
                ClassroomTypeMatch,
                EquipmentRequirement,
                TeacherPreference,
                TeacherScheduleCompactness
            };
        }

        /// <summary>
        /// Get constraints for K12 scheduling
        /// </summary>
        public static List<string> GetK12Constraints()
        {
            return new List<string>
            {
                // Core hard constraints
                TeacherConflict,
                ClassroomConflict,
                ClassroomCapacity,
                CourseAssignment,
                TimeContiguity,
                MinimumTimeGap,
                
                // Configurable hard constraints
                TeacherAvailability,
                ClassroomAvailability,
                CourseTimeRestriction,
                
                // Important soft constraints
                CourseDistribution,
                TeacherWorkload,
                StudentScheduleQuality
            };
        }

        /// <summary>
        /// Get constraints for exam scheduling
        /// </summary>
        public static List<string> GetExamConstraints()
        {
            return new List<string>
            {
                // Core hard constraints
                TeacherConflict, // Invigilation teacher
                ClassroomConflict,
                StudentConflict,
                ClassroomCapacity,
                MutualExclusionTime,
                
                // Configurable hard constraints
                TeacherAvailability,
                ClassroomAvailability,
                
                // Important soft constraints
                MinimumTimeGap, // Exam interval
                BuildingCapacityBalance
            };
        }

        /// <summary>
        /// Get constraint hierarchy
        /// </summary>
        public static ConstraintHierarchy GetConstraintHierarchy(string constraintId)
        {
            if (GetCoreHardConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level1_CoreHard;
            }
            else if (GetConfigurableHardConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level2_ConfigurableHard;
            }
            else if (GetPhysicalSoftConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level3_PhysicalSoft;
            }
            else if (GetQualitySoftConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level4_QualitySoft;
            }
            else
            {
                throw new ArgumentException($"Unknown constraint ID: {constraintId}");
            }
        }

        /// <summary>
        /// Check if constraint is a hard constraint
        /// </summary>
        public static bool IsHardConstraint(string constraintId)
        {
            var hierarchy = GetConstraintHierarchy(constraintId);
            return hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                   hierarchy == ConstraintHierarchy.Level2_ConfigurableHard;
        }

        /// <summary>
        /// Map BasicSchedulingRules to specific constraints
        /// </summary>
        public static List<string> MapBasicRuleToConstraints(string basicRule)
        {
            switch (basicRule)
            {
                case BasicSchedulingRules.ResourceConflictAvoidance:
                    return new List<string> { TeacherConflict, ClassroomConflict, StudentConflict };
                
                case BasicSchedulingRules.ResourceCapacityRespect:
                    return new List<string> { ClassroomCapacity };
                
                case BasicSchedulingRules.AvailabilityRespect:
                    return new List<string> { CourseAssignment };
                
                case BasicSchedulingRules.ResourceAvailability:
                    return new List<string> { TimeContiguity };
                
                case BasicSchedulingRules.ResourcePreference:
                    return new List<string> { CourseSectionConsistency, RelatedCourses };
                
                default:
                    throw new ArgumentException($"Unknown basic rule: {basicRule}");
            }
        }
    }

    /// <summary>
    /// Basic scheduling rules, defining common high-level scheduling guidelines
    /// </summary>
    public static class BasicSchedulingRules
    {
        /// <summary>
        /// Resource conflict avoidance rule - The same resource cannot be used multiple times in the same time slot
        /// </summary>
        public const string ResourceConflictAvoidance = "ResourceConflictAvoidance";

        /// <summary>
        /// Resource capacity respect rule - Resource capacity must meet demand
        /// </summary>
        public const string ResourceCapacityRespect = "ResourceCapacityRespect";

        /// <summary>
        /// Availability respect rule - Resources can only be used during their available time slots
        /// </summary>
        public const string AvailabilityRespect = "AvailabilityRespect";

        /// <summary>
        /// Resource preference rule - Respect resource preferences
        /// </summary>
        public const string ResourcePreference = "ResourcePreference";

        /// <summary>
        /// Resource availability rule - Resources should only be used when available
        /// </summary>
        public const string ResourceAvailability = "ResourceAvailability";
        
        /// <summary>
        /// Teacher preference rule - Respect teacher preferences
        /// </summary>
        public const string TeacherPreference = "TeacherPreference";

        /// <summary>
        /// Get the minimal set of essential core rules
        /// </summary>
        public static List<string> GetEssentialRules()
        {
            return new List<string>
            {
                ResourceConflictAvoidance,
                ResourceCapacityRespect
            };
        }

        /// <summary>
        /// Get the full list of basic rules
        /// </summary>
        public static List<string> GetAllRules()
        {
            return new List<string>
            {
                ResourceConflictAvoidance,
                ResourceCapacityRespect,
                AvailabilityRespect,
                ResourcePreference,
                ResourceAvailability
            };
        }
    }
} 