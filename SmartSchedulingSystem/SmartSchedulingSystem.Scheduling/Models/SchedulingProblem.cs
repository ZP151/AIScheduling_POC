using System;
using System.Collections.Generic;
using System.Data;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Data.Entities;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// Represents a course scheduling problem to be solved, containing all necessary input data
    /// </summary>
    public class SchedulingProblem
    {
        /// <summary>
        /// Unique ID of the problem
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Problem name or description
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Semester ID
        /// </summary>
        public int SemesterId { get; set; }

        /// <summary>
        /// List of course sections to be scheduled
        /// </summary>
        public List<CourseSectionInfo> CourseSections { get; set; } = new List<CourseSectionInfo>();

        /// <summary>
        /// List of available teachers
        /// </summary>
        public List<TeacherInfo> Teachers { get; set; } = new List<TeacherInfo>();

        /// <summary>
        /// List of available classrooms
        /// </summary>
        public List<ClassroomInfo> Classrooms { get; set; } = new List<ClassroomInfo>();

        /// <summary>
        /// List of available time slots
        /// </summary>
        public List<TimeSlotInfo> TimeSlots { get; set; } = new List<TimeSlotInfo>();

        /// <summary>
        /// Teacher course capability and preference mapping
        /// </summary>
        public List<TeacherCoursePreference> TeacherCoursePreferences { get; set; } = new List<TeacherCoursePreference>();

        /// <summary>
        /// Teacher availability
        /// </summary>
        public List<TeacherAvailability> TeacherAvailabilities { get; set; } = new List<TeacherAvailability>();
        
        /// <summary>
        /// Classroom availability
        /// </summary>
        public List<ClassroomAvailability> ClassroomAvailabilities { get; set; } = new List<ClassroomAvailability>();
        
        /// <summary>
        /// List of course resource requirements
        /// </summary>
        public List<CourseResourceRequirement> CourseResourceRequirements { get; set; } = new List<CourseResourceRequirement>();
        
        /// <summary>
        /// List of classroom resource information
        /// </summary>
        public List<ClassroomResource> ClassroomResources { get; set; } = new List<ClassroomResource>();
        
        /// <summary>
        /// Matching scores between classroom types and course types
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> RoomTypeMatchingScores { get; set; } = new Dictionary<string, Dictionary<string, double>>();
        
        /// <summary>
        /// List of scheduling constraints
        /// </summary>
        public List<IConstraint> Constraints { get; set; } = new List<IConstraint>();
        
        /// <summary>
        /// Whether to generate multiple solutions
        /// </summary>
        public bool GenerateMultipleSolutions { get; set; } = true;

        /// <summary>
        /// Number of scheduling solutions to generate
        /// </summary>
        public int SolutionCount { get; set; } = 3;
        
        /// <summary>
        /// List of course prerequisites
        /// </summary>
        public List<CoursePrerequisite> Prerequisites { get;  set; }

        /// <summary>
        /// Validate the problem's validity based on input data
        /// </summary>
        /// <returns>List of error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (CourseSections == null || CourseSections.Count == 0)
                errors.Add("No course sections to schedule");

            if (Teachers == null || Teachers.Count == 0)
                errors.Add("No teachers available");

            if (Classrooms == null || Classrooms.Count == 0)
                errors.Add("No classrooms available");

            if (TimeSlots == null || TimeSlots.Count == 0)
                errors.Add("No time slots defined");

            // Validate teacher and course matching
            if (TeacherCoursePreferences != null && TeacherCoursePreferences.Count > 0)
            {
                var teacherIds = Teachers.ConvertAll(t => t.Id);
                var courseIds = CourseSections.ConvertAll(c => c.CourseId);

                foreach (var preference in TeacherCoursePreferences)
                {
                    if (!teacherIds.Contains(preference.TeacherId))
                        errors.Add($"Teacher ID {preference.TeacherId} in preferences does not exist");

                    if (!courseIds.Contains(preference.CourseId))
                        errors.Add($"Course ID {preference.CourseId} in preferences does not exist");
                }
            }

            // Validate teacher time availability
            if (TeacherAvailabilities != null && TeacherAvailabilities.Count > 0)
            {
                var teacherIds = Teachers.ConvertAll(t => t.Id);
                var timeSlotIds = TimeSlots.ConvertAll(t => t.Id);

                foreach (var availability in TeacherAvailabilities)
                {
                    if (!teacherIds.Contains(availability.TeacherId))
                        errors.Add($"Teacher ID {availability.TeacherId} in availabilities does not exist");

                    if (!timeSlotIds.Contains(availability.TimeSlotId))
                        errors.Add($"Time slot ID {availability.TimeSlotId} in teacher availabilities does not exist");
                }
            }

            // Validate classroom time availability
            if (ClassroomAvailabilities != null && ClassroomAvailabilities.Count > 0)
            {
                var classroomIds = Classrooms.ConvertAll(c => c.Id);
                var timeSlotIds = TimeSlots.ConvertAll(t => t.Id);

                foreach (var availability in ClassroomAvailabilities)
                {
                    if (!classroomIds.Contains(availability.ClassroomId))
                        errors.Add($"Classroom ID {availability.ClassroomId} in availabilities does not exist");

                    if (!timeSlotIds.Contains(availability.TimeSlotId))
                        errors.Add($"Time slot ID {availability.TimeSlotId} in classroom availabilities does not exist");
                }
            }

            return errors;
        }
    }
    
    /// <summary>
    /// Course section information
    /// </summary>
    public class CourseSectionInfo
    {
        /// <summary>
        /// Section ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Course ID
        /// </summary>
        public int CourseId { get; set; }
        public Course Course { get; set; }

        /// <summary>
        /// Course code
        /// </summary>
        public string CourseCode { get; set; }

        /// <summary>
        /// Course name
        /// </summary>
        public string CourseName { get; set; }

        /// <summary>
        /// Section code
        /// </summary>
        public string SectionCode { get; set; }

        /// <summary>
        /// Credits
        /// </summary>
        public int Credits { get; set; }

        /// <summary>
        /// Total hours
        /// </summary>
        public int Hours { get; set; }
        
        /// <summary>
        /// Weekly hours
        /// </summary>
        public int WeeklyHours { get; set; }

        /// <summary>
        /// Sessions per week
        /// </summary>
        public int SessionsPerWeek { get; set; }

        /// <summary>
        /// Hours per session
        /// </summary>
        public double HoursPerSession { get; set; }

        /// <summary>
        /// Student capacity for this course
        /// Variable name should be changed to course student capacity later
        /// </summary>
        public int Enrollment { get; set; }

        /// <summary>
        /// Department ID
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Department name
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Course type (theory, laboratory, practical, etc.)
        /// </summary>
        public string CourseType { get; set; }

        /// <summary>
        /// Required classroom type
        /// </summary>
        public string RequiredRoomType { get; set; }

        /// <summary>
        /// Required classroom type (synonym with RequiredRoomType)
        /// </summary>
        public string RequiredClassroomType { get => RequiredRoomType; set => RequiredRoomType = value; }

        /// <summary>
        /// Required equipment
        /// </summary>
        public string RequiredEquipment { get; set; }

        /// <summary>
        /// Whether the same teacher is required for all sessions
        /// </summary>
        public bool RequiresSameTeacher { get; set; }

        /// <summary>
        /// Whether the same room is required for all sessions
        /// </summary>
        public bool RequiresSameRoom { get; set; }

        /// <summary>
        /// Cross-listed course section ID
        /// </summary>
        public int? CrossListedWithId { get; set; }

        /// <summary>
        /// Course difficulty level (1-5)
        /// </summary>
        public int DifficultyLevel { get; set; } = 3;
    }

    /// <summary>
    /// Teacher information
    /// </summary>
    public class TeacherInfo
    {
        /// <summary>
        /// Teacher ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Teacher name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Academic title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Department ID
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Department name
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Maximum weekly teaching hours
        /// </summary>
        public int MaxWeeklyHours { get; set; }

        /// <summary>
        /// Maximum daily teaching hours
        /// </summary>
        public int MaxDailyHours { get; set; }

        /// <summary>
        /// Maximum consecutive teaching hours
        /// </summary>
        public int MaxConsecutiveHours { get; set; }

        /// <summary>
        /// Preferred building for teaching
        /// </summary>
        public string PreferredBuilding { get; set; }
    }

    /// <summary>
    /// Classroom information
    /// </summary>
    public class ClassroomInfo
    {
        /// <summary>
        /// Classroom ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Classroom name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Building name
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// Campus ID
        /// </summary>
        public int CampusId { get; set; }

        /// <summary>
        /// Campus name
        /// </summary>
        public string CampusName { get; set; }

        /// <summary>
        /// Room capacity
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Room type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Classroom type (synonym with Type)
        /// </summary>
        public string ClassroomType { get => Type; set => Type = value; }

        /// <summary>
        /// Room type (synonym with Type)
        /// </summary>
        public string RoomType { get => Type; set => Type = value; }

        /// <summary>
        /// Available equipment
        /// </summary>
        public string Equipment { get; set; }

        /// <summary>
        /// Whether the room has computers
        /// </summary>
        public bool HasComputers { get; set; }

        /// <summary>
        /// Whether the room has a projector
        /// </summary>
        public bool HasProjector { get; set; }
    }

    /// <summary>
    /// Time slot information
    /// </summary>
    public class TimeSlotInfo
    {
        /// <summary>
        /// Time slot ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Day of week (1-7)
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// Day name
        /// </summary>
        public string DayName { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Whether the time slot is available
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Display format for the time slot
        /// </summary>
        public string Display => $"{DayName} {StartTime:hh\\:mm}-{EndTime:hh\\:mm}";

        /// <summary>
        /// Time slot type
        /// </summary>
        public string Type { get; set; } = "Regular";
    }

    /// <summary>
    /// Course prerequisite information
    /// </summary>
    public class CoursePrerequisite
    {
        /// <summary>
        /// Prerequisite ID
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// Course ID
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Current course
        /// </summary>
        public Course Course { get; set; }

        /// <summary>
        /// Prerequisite course ID
        /// </summary>
        public int PrerequisiteCourseId { get; set; }
    }

    /// <summary>
    /// Teacher course preference information
    /// </summary>
    public class TeacherCoursePreference
    {
        /// <summary>
        /// Teacher ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// Course ID
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Teacher's proficiency level for this course
        /// </summary>
        public int ProficiencyLevel { get; set; }

        /// <summary>
        /// Teacher's preference level for this course
        /// </summary>
        public int PreferenceLevel { get; set; }
    }

    /// <summary>
    /// Teacher availability information
    /// </summary>
    public class TeacherAvailability
    {
        /// <summary>
        /// Teacher ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// Teacher name
        /// </summary>
        public string TeacherName { get; set; }

        /// <summary>
        /// Time slot ID
        /// </summary>
        public int TimeSlotId { get; set; }

        /// <summary>
        /// Day of week (1-7)
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Whether the teacher is available
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Reason for unavailability
        /// </summary>
        public string UnavailableReason { get; set; }

        /// <summary>
        /// Teacher's preference level for this time slot
        /// </summary>
        public int PreferenceLevel { get; set; }

        /// <summary>
        /// Weeks when this availability applies
        /// </summary>
        public List<int> ApplicableWeeks { get; set; } = new List<int>();
    }

    /// <summary>
    /// Classroom availability information
    /// </summary>
    public class ClassroomAvailability
    {
        /// <summary>
        /// Classroom ID
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// Classroom name
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// Building name
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// Time slot ID
        /// </summary>
        public int TimeSlotId { get; set; }

        /// <summary>
        /// Day of week (1-7)
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Whether the classroom is available
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Reason for unavailability
        /// </summary>
        public string UnavailableReason { get; set; }

        /// <summary>
        /// Weeks when this availability applies
        /// </summary>
        public List<int> ApplicableWeeks { get; set; } = new List<int>();
    }

    /// <summary>
    /// Course resource requirement information
    /// </summary>
    public class CourseResourceRequirement
    {
        /// <summary>
        /// Course section ID
        /// </summary>
        public int CourseSectionId { get; set; }

        /// <summary>
        /// Course code
        /// </summary>
        public string CourseCode { get; set; }

        /// <summary>
        /// Course name
        /// </summary>
        public string CourseName { get; set; }

        /// <summary>
        /// Resource types required by the course
        /// </summary>
        public List<string> ResourceTypes { get; set; } = new List<string>();

        /// <summary>
        /// Preferred room types for the course
        /// </summary>
        public List<string> PreferredRoomTypes { get; set; } = new List<string>();
        
        /// <summary>
        /// Required equipment for the course
        /// </summary>
        public List<string> RequiredEquipment { get => ResourceTypes; set => ResourceTypes = value; }

        /// <summary>
        /// Required seating capacity
        /// </summary>
        public int RequiredCapacity { get; set; }

        /// <summary>
        /// Weight for resource matching in scoring
        /// </summary>
        public double ResourceMatchingWeight { get; set; } = 0.8;
    }

    /// <summary>
    /// Classroom resource information
    /// </summary>
    public class ClassroomResource
    {
        /// <summary>
        /// Classroom ID
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// Classroom name
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// Building name
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// Resource types available in the classroom
        /// </summary>
        public List<string> ResourceTypes { get; set; } = new List<string>();
        
        /// <summary>
        /// Available equipment in the classroom
        /// </summary>
        public List<string> AvailableEquipment { get => ResourceTypes; set => ResourceTypes = value; }

        /// <summary>
        /// Room type
        /// </summary>
        public string RoomType { get; set; }

        /// <summary>
        /// Room capacity
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Weight for resource utilization in scoring
        /// </summary>
        public double ResourceUtilizationWeight { get; set; } = 0.7;

        /// <summary>
        /// Weight for capacity utilization in scoring
        /// </summary>
        public double CapacityUtilizationWeight { get; set; } = 0.3;
    }
}