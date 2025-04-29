using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    /// <summary>
    /// Constraint category constants for classifying constraints
    /// </summary>
    public static class ConstraintCategory
    {
        /// <summary>
        /// Resource allocation category - Constraints related to allocation of physical resources like teachers and classrooms
        /// </summary>
        public const string ResourceAllocation = "ResourceAllocation";

        /// <summary>
        /// Time allocation category - Constraints related to time slot allocation and time-based restrictions
        /// </summary>
        public const string TimeAllocation = "TimeAllocation";

        /// <summary>
        /// Teaching quality category - Constraints related to teaching quality and effectiveness
        /// </summary>
        public const string TeachingQuality = "TeachingQuality";

        /// <summary>
        /// Student experience category - Constraints related to student experience and schedule quality
        /// </summary>
        public const string StudentExperience = "StudentExperience";

        /// <summary>
        /// Core rules category - Basic scheduling rules and restrictions
        /// </summary>
        public const string CoreRules = "CoreRules";

        /// <summary>
        /// Administrative category - Constraints related to management and administrative aspects
        /// </summary>
        public const string Administrative = "Administrative";
    }

    /// <summary>
    /// Constraint type enumeration
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        /// Hard constraint - Must be satisfied
        /// </summary>
        Hard,

        /// <summary>
        /// Soft constraint - Should be satisfied when possible
        /// </summary>
        Soft
    }

    /// <summary>
    /// Constraint hierarchy levels
    /// </summary>
    public enum ConstraintHierarchy
    {
        /// <summary>
        /// Level 1: Core hard constraints - Basic scheduling rules that must be satisfied
        /// </summary>
        Level1_CoreHard = 1,

        /// <summary>
        /// Level 2: Configurable hard constraints - Must be satisfied but can be configured
        /// </summary>
        Level2_ConfigurableHard = 2,

        /// <summary>
        /// Level 3: Physical soft constraints - Related to physical resource limitations, should be satisfied when possible
        /// </summary>
        Level3_PhysicalSoft = 3,

        /// <summary>
        /// Level 4: Quality soft constraints - Related to quality and experience, should be satisfied when possible
        /// </summary>
        Level4_QualitySoft = 4
    }
} 