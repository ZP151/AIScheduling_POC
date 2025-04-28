using SmartSchedulingSystem.Scheduling.Constraints;

using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System.Collections.Generic;
using System.Data;

namespace SmartSchedulingSystem.Test
{
    /// <summary>
    /// Test data generator extension, adding constraint initialization functionality
    /// </summary>
    public static class TestDataGeneratorExtended
    {
        /// <summary>
        /// Generate test scheduling problem with necessary constraints
        /// </summary>
        public static SchedulingProblem GenerateTestProblemWithConstraints()
        {
            // Generate base problem data
            var problem = TestDataGenerator.GenerateTestProblem();

            // Add constraints
            InitializeConstraints(problem);

            return problem;
        }

        /// <summary>
        /// Initialize constraint list
        /// </summary>
        private static void InitializeConstraints(SchedulingProblem problem)
        {
            // 1. Add hard constraints
            AddHardConstraints(problem);

            // 2. Add physical soft constraints
            AddPhysicalSoftConstraints(problem);

            // 3. Add quality soft constraints
            AddQualitySoftConstraints(problem);
        }

        /// <summary>
        /// Add hard constraints
        /// </summary>
        private static void AddHardConstraints(SchedulingProblem problem)
        {
            // Teacher conflict constraint
            problem.Constraints.Add(new TeacherConflictConstraint());

            // Classroom conflict constraint
            problem.Constraints.Add(new ClassroomConflictConstraint());

            // Teacher availability constraint
            problem.Constraints.Add(new TeacherAvailabilityConstraint());
        }

        /// <summary>
        /// Add physical soft constraints
        /// </summary>
        private static void AddPhysicalSoftConstraints(SchedulingProblem problem)
        {
            // Classroom type matching constraint
            var courseSectionTypes = GetCourseSectionTypes(problem.CourseSections);
            var classroomTypes = GetClassroomTypes(problem.Classrooms);

            // Equipment requirement constraint
            var sectionRequiredEquipment = GetSectionRequiredEquipment(problem.CourseSections);
            var classroomEquipment = GetClassroomEquipment(problem.Classrooms);
            problem.Constraints.Add(new EquipmentRequirementConstraint(sectionRequiredEquipment, classroomEquipment));

            // Time availability constraint
            var unavailablePeriods = new List<(DateTime Start, DateTime End, string Reason)>();
            var semesterDates = new Dictionary<int, (DateTime Start, DateTime End)>
            {
                { problem.Id, (DateTime.Now.Date, DateTime.Now.Date.AddMonths(4)) }
            };
            problem.Constraints.Add(new TimeAvailabilityConstraint(unavailablePeriods, semesterDates));

            // Location proximity constraint
            var teacherDepartmentIds = GetTeacherDepartmentIds(problem.Teachers);
            var buildingCampusIds = GetBuildingCampusIds(problem.Classrooms);
            var campusTravelTimes = GetCampusTravelTimes();
            problem.Constraints.Add(new LocationProximityConstraint(teacherDepartmentIds, buildingCampusIds, campusTravelTimes));
        }

        /// <summary>
        /// Add quality soft constraints
        /// </summary>
        private static void AddQualitySoftConstraints(SchedulingProblem problem)
        {
            // Teacher preference constraint
            var teacherPreferences = ConvertTeacherPreferences(problem.TeacherAvailabilities);
            problem.Constraints.Add(new TeacherPreferenceConstraint(teacherPreferences));

            // Teacher workload constraint
            var maxWeeklyHours = GetTeacherMaxWeeklyHours(problem.Teachers);
            var maxDailyHours = GetTeacherMaxDailyHours(problem.Teachers);
            problem.Constraints.Add(new TeacherWorkloadConstraint(maxWeeklyHours, maxDailyHours));

            // Teacher schedule compactness constraint
            problem.Constraints.Add(new TeacherScheduleCompactnessConstraint());
        }

        #region Helper Methods for Constraint Initialization

        private static Dictionary<(int TeacherId, int TimeSlotId), bool> ConvertTeacherAvailabilities(
            List<TeacherAvailability> availabilities)
        {
            var result = new Dictionary<(int TeacherId, int TimeSlotId), bool>();

            foreach (var availability in availabilities)
            {
                result[(availability.TeacherId, availability.TimeSlotId)] = availability.IsAvailable;
            }

            return result;
        }

        private static Dictionary<(int ClassroomId, int TimeSlotId), bool> ConvertClassroomAvailabilities(
            List<ClassroomAvailability> availabilities)
        {
            var result = new Dictionary<(int ClassroomId, int TimeSlotId), bool>();

            foreach (var availability in availabilities)
            {
                result[(availability.ClassroomId, availability.TimeSlotId)] = availability.IsAvailable;
            }

            return result;
        }

        private static Dictionary<int, int> GetClassroomCapacities(List<ClassroomInfo> classrooms)
        {
            return classrooms.ToDictionary(c => c.Id, c => c.Capacity);
        }

        private static Dictionary<int, int> GetExpectedEnrollments(List<CourseSectionInfo> sections)
        {
            return sections.ToDictionary(s => s.Id, s => s.Enrollment);
        }

        private static Dictionary<int, int> GetCourseSectionMap(List<CourseSectionInfo> sections)
        {
            return sections.ToDictionary(s => s.Id, s => s.CourseId);
        }

        private static Dictionary<int, string> GetCourseSectionTypes(List<CourseSectionInfo> sections)
        {
            return sections.ToDictionary(s => s.Id, s => s.CourseType);
        }

        private static Dictionary<int, string> GetClassroomTypes(List<ClassroomInfo> classrooms)
        {
            return classrooms.ToDictionary(c => c.Id, c => c.Type);
        }

        private static Dictionary<int, List<string>> GetSectionRequiredEquipment(List<CourseSectionInfo> sections)
        {
            var result = new Dictionary<int, List<string>>();

            foreach (var section in sections)
            {
                if (!string.IsNullOrEmpty(section.RequiredEquipment))
                {
                    result[section.Id] = section.RequiredEquipment.Split(',').Select(e => e.Trim()).ToList();
                }
                else
                {
                    result[section.Id] = new List<string>();
                }
            }

            return result;
        }

        private static Dictionary<int, List<string>> GetClassroomEquipment(List<ClassroomInfo> classrooms)
        {
            var result = new Dictionary<int, List<string>>();

            foreach (var classroom in classrooms)
            {
                if (!string.IsNullOrEmpty(classroom.Equipment))
                {
                    result[classroom.Id] = classroom.Equipment.Split(',').Select(e => e.Trim()).ToList();
                }
                else
                {
                    result[classroom.Id] = new List<string>();
                }
            }

            return result;
        }

        private static Dictionary<int, int> GetTeacherDepartmentIds(List<TeacherInfo> teachers)
        {
            return teachers.ToDictionary(t => t.Id, t => t.DepartmentId);
        }

        private static Dictionary<int, int> GetBuildingCampusIds(List<ClassroomInfo> classrooms)
        {
            // Assume each classroom has a campus ID property
            return classrooms.ToDictionary(c => c.Id, c => c.CampusId);
        }

        private static Dictionary<(int, int), int> GetCampusTravelTimes()
        {
            // Simplified version: only two campuses, 30 minutes travel time between them
            var result = new Dictionary<(int, int), int>
            {
                { (1, 2), 30 },
                { (2, 1), 30 }
            };

            return result;
        }

        private static Dictionary<(int TeacherId, int TimeSlotId), int> ConvertTeacherPreferences(
            List<TeacherAvailability> availabilities)
        {
            var result = new Dictionary<(int TeacherId, int TimeSlotId), int>();

            foreach (var availability in availabilities)
            {
                if (availability.IsAvailable) // Only record preferences for available time slots
                {
                    result[(availability.TeacherId, availability.TimeSlotId)] = availability.PreferenceLevel;
                }
            }

            return result;
        }

        private static Dictionary<int, int> GetTeacherMaxWeeklyHours(List<TeacherInfo> teachers)
        {
            return teachers.ToDictionary(t => t.Id, t => t.MaxWeeklyHours);
        }

        private static Dictionary<int, int> GetTeacherMaxDailyHours(List<TeacherInfo> teachers)
        {
            return teachers.ToDictionary(t => t.Id, t => t.MaxDailyHours);
        }

        #endregion
    }
}