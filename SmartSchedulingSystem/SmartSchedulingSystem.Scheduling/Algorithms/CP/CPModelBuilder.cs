using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// Tool class for building CP models, responsible for creating models used by constraint programming solvers
    /// </summary>
    public class CPModelBuilder
    {
        private readonly IEnumerable<ICPConstraintConverter> _constraintConverters;
        private readonly ConstraintManager _constraintManager;
        private Dictionary<string, IntVar> _variables = new Dictionary<string, IntVar>();
        private readonly ILogger<CPModelBuilder> _logger;
        public CPModelBuilder(IEnumerable<ICPConstraintConverter> constraintConverters, ConstraintManager constraintManager, ILogger<CPModelBuilder> logger)
        {
            _constraintConverters = constraintConverters ?? throw new ArgumentNullException(nameof(constraintConverters));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }
        public Dictionary<string, IntVar> GetVariables()
        {
            return _variables;
        }
        /// <summary>
        /// Build CP model for scheduling problem
        /// </summary>
        public CpModel BuildModel(SchedulingProblem problem, ConstraintApplicationLevel level)
        {
            _variables.Clear();

            Console.WriteLine("============ CP Model Building Started ============");
            Console.WriteLine($"Problem details: {problem.Name}, {problem.CourseSections.Count} courses");
            Console.WriteLine($"Constraint application level: {level}");

            if (problem == null)
                throw new ArgumentNullException(nameof(problem));

            var model = new CpModel();
            _variables = CreateDecisionVariables(model, problem);

            // Add core constraints (Level1_CoreHard)
            Console.WriteLine("Adding OneCourseOneAssignment constraint");
            AddOneCourseOneAssignmentConstraints(model, _variables, problem);  // Each course must be assigned once
            
            Console.WriteLine("Adding teacher conflict constraint");
            AddTeacherConflictConstraints(model, _variables, problem);   // Teacher cannot teach two courses at the same time
            
            Console.WriteLine("Adding classroom conflict constraint");
            AddClassroomConflictConstraints(model, _variables, problem); // Classroom cannot be used for two courses at the same time

            // Selectively add constraints based on constraint level
            if (level >= ConstraintApplicationLevel.Basic)
            {
                Console.WriteLine("Adding classroom capacity constraint");
                AddClassroomCapacityConstraints(model, _variables, problem); // Classroom capacity constraint
                
                Console.WriteLine("Adding prerequisite constraint");
                AddPrerequisiteConstraints(model, _variables, problem);      // Prerequisite constraint
            }

            // Add constraints at higher levels (Level2 and above)
            if (level >= ConstraintApplicationLevel.Standard)
            {
                Console.WriteLine("Adding teacher availability constraint");
                AddTeacherAvailabilityConstraints(model, _variables, problem); // Teacher availability constraint (Level2)
                
                Console.WriteLine("Adding classroom availability constraint");
                AddClassroomAvailabilityConstraints(model, _variables, problem); // Classroom availability constraint (Level2)
            }

            // Apply custom constraint converters allowed by the current constraint level
            foreach (var converter in _constraintConverters)
            {
                // Only apply constraint converters allowed at current level
                if (IsConverterAllowedAtLevel(converter, level))
                {
                    Console.WriteLine($"Applying constraint converter: {converter.GetType().Name}");
                    converter.AddToModel(model, _variables, problem);
                }
            }

            // Set objective function (maximize soft constraint satisfaction)
            Console.WriteLine("Setting objective function");
            SetupObjectiveFunction(model, _variables, problem);
            Console.WriteLine("============ CP Model Building Completed ============");

            return model;
        }

        /// <summary>
        /// Determine if the constraint converter is allowed to apply at the current constraint level
        /// </summary>
        private bool IsConverterAllowedAtLevel(ICPConstraintConverter converter, ConstraintApplicationLevel level)
        {
            // Determine the constraint level of the constraint converter based on its type
            string converterName = converter.GetType().Name;
            
            // Core hard constraint converters (Level1) - Allowed at all levels
            if (converterName.Contains("TeacherConflict") || 
                converterName.Contains("ClassroomConflict") ||
                converterName.Contains("ClassroomCapacity") ||
                converterName.Contains("Prerequisite"))
            {
                return true;
            }
            
            // Variable hard constraint converters (Level2) - Only allowed at Standard and above levels
            if (level >= ConstraintApplicationLevel.Standard &&
                (converterName.Contains("TeacherAvailability") || 
                 converterName.Contains("ClassroomAvailability")))
            {
                return true;
            }
            
            // Soft constraint converters (Level3 and Level4) - Only allowed at Complete level
            if (level >= ConstraintApplicationLevel.Complete)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Create decision variables - Optimized version, reduce variable count to improve solving speed
        /// </summary>
        private Dictionary<string, IntVar> CreateDecisionVariables(CpModel model, SchedulingProblem problem)
        {
            var variables = new Dictionary<string, IntVar>();
            int variableCount = 0;

            _logger.LogInformation($"Starting to create decision variables: Course count={problem.CourseSections.Count}, " +
                                 $"Teacher count={problem.Teachers.Count}, Classroom count={problem.Classrooms.Count}, " +
                                 $"Time slot count={problem.TimeSlots.Count}");

            // Check basic conditions
            if (problem.CourseSections.Count == 0 || problem.Teachers.Count == 0 ||
                problem.Classrooms.Count == 0 || problem.TimeSlots.Count == 0)
            {
                _logger.LogError("Variable creation failed: Course, Teacher, Classroom, or Time slot count is 0");
                return variables;
            }

            // Create variables for each course - Pre-filtering method to reduce variable count
            foreach (var section in problem.CourseSections)
            {
                bool sectionHasVariables = false;
                _logger.LogDebug($"Creating variables for course {section.Id} ({section.CourseName})...");

                // Filter classrooms that meet capacity requirements to reduce variable count
                var suitableRooms = problem.Classrooms
                    .Where(room => room.Capacity >= section.Enrollment)
                    .ToList();

                if (suitableRooms.Count == 0)
                {
                    _logger.LogWarning($"Warning: Course {section.Id} ({section.CourseName}) enrollment is {section.Enrollment}, no suitable room found");
                    
                    // If no suitable room is found, select the largest few rooms
                    suitableRooms = problem.Classrooms
                        .OrderByDescending(room => room.Capacity)
                        .Take(3)
                        .ToList();
                    
                    _logger.LogWarning($"To avoid no solution, selected {suitableRooms.Count} rooms with the largest capacity");
                }

                // Filter teachers qualified to teach this course to reduce variable count
                var qualifiedTeachers = new List<TeacherInfo>();
                
                // Find teacher course preferences
                var teacherPreferences = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 2)
                    .ToList();
                
                if (teacherPreferences.Count > 0)
                {
                    // Select teachers based on preferences
                    var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                    qualifiedTeachers = problem.Teachers
                        .Where(t => preferredTeacherIds.Contains(t.Id))
                        .ToList();
                }
                
                if (qualifiedTeachers.Count == 0)
                {
                    _logger.LogWarning($"Warning: Course {section.Id} ({section.CourseName}) no qualified teacher found");
                    
                    // If no qualified teacher is found, select all teachers to avoid no solution
                    qualifiedTeachers = problem.Teachers.ToList();
                    _logger.LogWarning($"To avoid no solution, selected all {qualifiedTeachers.Count} teachers");
                }

                foreach (var timeSlot in problem.TimeSlots)
                {
                    // Initially ignore availability constraints to generate more possible variables
                    var availableRooms = suitableRooms;
                    var availableTeachers = qualifiedTeachers;

                    foreach (var classroom in availableRooms)
                    {
                        foreach (var teacher in availableTeachers)
                        {
                            // Create variables
                            string varName = $"c{section.Id}_t{timeSlot.Id}_r{classroom.Id}_f{teacher.Id}";
                            var variable = model.NewBoolVar(varName);
                            variables[varName] = variable;
                            variableCount++;
                            sectionHasVariables = true;

                            if (variableCount % 1000 == 0)
                            {
                                _logger.LogInformation($"Created {variableCount} variables...");
                            }
                        }
                    }
                }

                if (!sectionHasVariables)
                {
                    _logger.LogWarning($"Course {section.Id} ({section.CourseName}) no variables created, may not generate valid solution");
                }
            }

            _logger.LogInformation($"Variable creation completed, total {variables.Count} variables created");
            return variables;
        }

        /// <summary>
        /// Set objective function, optimize soft constraint satisfaction
        /// </summary>
        private void SetupObjectiveFunction(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            // Create list of objective function terms
            var terms = new List<IntVar>();
            var coefficients = new List<int>();

            int objectiveConstant = 0;

            // 1. Preference matching items - Teacher and course matching score
            foreach (var section in problem.CourseSections)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    foreach (var classroom in problem.Classrooms)
                    {
                        // Calculate the matching score between the classroom type and the course requirement
                        int roomTypeScore = CalculateRoomTypeMatchScore(section, classroom, problem);

                        // Calculate the matching score between the classroom capacity and the course enrollment
                        int capacityScore = CalculateCapacityScore(section.Enrollment, classroom.Capacity);

                        // Evaluate time slot preferences
                        int timeSlotScore = 10; // Default score
                        
                        // Remove the special weight setting for evening time slots, so all time slots have the same weight
                        // No longer distinguish between morning, afternoon, and evening time slots, treat each time slot equally

                        foreach (var teacher in problem.Teachers)
                        {
                            string varName = $"c{section.Id}_t{timeSlot.Id}_r{classroom.Id}_f{teacher.Id}";
                            if (_variables.TryGetValue(varName, out var variable))
                            {
                                // Calculate the teacher's preference score for this course
                                int teacherPreferenceScore = 0;
                                var preference = problem.TeacherCoursePreferences
                                    .FirstOrDefault(tcp => tcp.TeacherId == teacher.Id && tcp.CourseId == section.CourseId);

                                if (preference != null)
                                {
                                    // Calculate the score based on the teacher's professional level and preference
                                    teacherPreferenceScore = preference.ProficiencyLevel * 5 + preference.PreferenceLevel * 2;
                                }

                                // Add up all scores
                                int totalScore = teacherPreferenceScore + roomTypeScore + capacityScore + timeSlotScore;

                                terms.Add(variable);
                                coefficients.Add(totalScore);
                            }
                        }
                    }
                }
            }

            // 2. Add constraint preferences - Teacher workload balance, etc.
            
            // Add other objectives such as weekday balance

            // Set objective function
            if (terms.Count > 0)
            {
                model.Maximize(LinearExpr.WeightedSum(terms.ToArray(), coefficients.ToArray()) + objectiveConstant);
            }
        }

        /// <summary>
        /// Calculate the matching score between the course and the classroom type
        /// </summary>
        private int CalculateRoomTypeMatchScore(CourseSectionInfo course, ClassroomInfo classroom, SchedulingProblem problem)
        {
            // If the course has a classroom type requirement, but the type does not match
            if (!string.IsNullOrEmpty(course.RequiredRoomType) && 
                !string.IsNullOrEmpty(classroom.RoomType) && 
                !IsCompatibleRoomType(course.RequiredRoomType, classroom.RoomType))
            {
                return 0; // Not compatible
            }

            // Default to a normal classroom, any classroom is acceptable
            return 3;
        }

        /// <summary>
        /// Check if two classroom types are compatible
        /// </summary>
        private bool IsCompatibleRoomType(string requiredType, string actualType)
        {
            // Check common compatible types
            if (requiredType.Contains("lecture", StringComparison.OrdinalIgnoreCase))
            {
                // Lecture rooms can be in large classrooms, multimedia rooms, etc.
                return actualType.Contains("large", StringComparison.OrdinalIgnoreCase) ||
                       actualType.Contains("multimedia", StringComparison.OrdinalIgnoreCase);
            }

            if (requiredType.Contains("lab", StringComparison.OrdinalIgnoreCase))
            {
                // Lab courses must be in labs, cannot be substituted
                return actualType.Contains("lab", StringComparison.OrdinalIgnoreCase);
            }

            if (requiredType.Contains("computer", StringComparison.OrdinalIgnoreCase))
            {
                // Computer courses must be in computer rooms
                return actualType.Contains("computer", StringComparison.OrdinalIgnoreCase);
            }

            // Other cases are considered incompatible
            return false;
        }
        private void AddOneCourseOneAssignmentConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var course in problem.CourseSections)
            {
                Console.WriteLine("Adding constraint for each course must be assigned once");

                // Find all variables involving this course section
                var courseVars = _variables
                    .Where(kv => kv.Key.StartsWith($"c{course.Id}_"))
                    .Select(kv => kv.Value)
                    .ToList();
                Console.WriteLine($"Course {course.Id} found {courseVars.Count} variables");

                if (courseVars.Count > 0)
                {
                    // Constraint: Each course must be assigned exactly once
                    model.Add(LinearExpr.Sum(courseVars) == 1);
                    Console.WriteLine($"Added OneCourseOneAssignment constraint for course {course.Id}");
                }
                else
                {
                    Console.WriteLine($"Warning: Course {course.Id} no related variables found, cannot add constraint!");
                }
            }
        }

        private void AddTeacherConflictConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            _logger.LogDebug("Adding teacher conflict constraint...");

            // Pre-processing: Group variables by teacher and time slot
            var teacherTimeVarsMap = new Dictionary<(int teacherId, int timeSlotId), List<IntVar>>();

            foreach (var entry in variables)
            {
                string key = entry.Key;

                // Parse variable name "c{sectionId}_t{timeSlotId}_r{roomId}_f{teacherId}"
                var parts = key.Split('_');
                if (parts.Length < 4) continue;

                int timeSlotId = int.Parse(parts[1].Substring(1));
                int teacherId = int.Parse(parts[3].Substring(1));

                var mapKey = (teacherId, timeSlotId);
                if (!teacherTimeVarsMap.ContainsKey(mapKey))
                {
                    teacherTimeVarsMap[mapKey] = new List<IntVar>();
                }

                teacherTimeVarsMap[mapKey].Add(entry.Value);
            }

            // Batch add constraints -Maximum of one course taught by the same teacher in the same time slot
            int constraintCount = 0;
            foreach (var entry in teacherTimeVarsMap)
            {
                var conflictingVars = entry.Value;
                if (conflictingVars.Count > 1)
                {
                    model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    constraintCount++;
                }
            }

            _logger.LogDebug($"Added {constraintCount} teacher conflict constraints");
        }

        private void AddClassroomConflictConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            _logger.LogDebug("Adding classroom conflict constraint...");

            // Pre-processing: Group variables by classroom and time slot
            var roomTimeVarsMap = new Dictionary<(int roomId, int timeSlotId), List<IntVar>>();

            foreach (var entry in variables)
            {
                string key = entry.Key;

                // Parse variable name "c{sectionId}_t{timeSlotId}_r{roomId}_f{teacherId}"
                var parts = key.Split('_');
                if (parts.Length < 4) continue;

                int timeSlotId = int.Parse(parts[1].Substring(1));
                int roomId = int.Parse(parts[2].Substring(1));

                var mapKey = (roomId, timeSlotId);
                if (!roomTimeVarsMap.ContainsKey(mapKey))
                {
                    roomTimeVarsMap[mapKey] = new List<IntVar>();
                }

                roomTimeVarsMap[mapKey].Add(entry.Value);
            }

            // Batch add constraints - Maximum of one course in the same classroom in the same time slot
            int constraintCount = 0;
            foreach (var entry in roomTimeVarsMap)
            {
                var conflictingVars = entry.Value;
                if (conflictingVars.Count > 1)
                {
                    model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    constraintCount++;
                }
            }

            _logger.LogDebug($"Added {constraintCount} classroom conflict constraints");
        }

        private void AddTeacherAvailabilityConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var availability in problem.TeacherAvailabilities)
            {
                if (!availability.IsAvailable)
                {
                    // Find all variables involving this teacher in the unavailable time slot
                    var unavailableVars = _variables
                        .Where(kv => kv.Key.Contains($"_t{availability.TimeSlotId}_") &&
                                   kv.Key.EndsWith($"_f{availability.TeacherId}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    foreach (var variable in unavailableVars)
                    {
                        // Constraint: The variable for the unavailable time slot must be 0
                        model.Add(variable == 0);
                    }
                }
            }
        }

        private void AddClassroomAvailabilityConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var availability in problem.ClassroomAvailabilities)
            {
                if (!availability.IsAvailable)
                {
                    // Find all variables involving this classroom in the unavailable time slot
                    var unavailableVars = _variables
                        .Where(kv => kv.Key.Contains($"_t{availability.TimeSlotId}_r{availability.ClassroomId}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    foreach (var variable in unavailableVars)
                    {
                        // Constraint: The variable for the unavailable time slot must be 0
                        model.Add(variable == 0);
                    }
                }
            }
        }

        private void AddClassroomCapacityConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var section in problem.CourseSections)
            {
                foreach (var classroom in problem.Classrooms)
                {
                    if (classroom.Capacity < section.Enrollment)
                    {
                        // Find all variables involving this classroom with insufficient capacity
                        var invalidVars = _variables
                            .Where(kv => kv.Key.StartsWith($"c{section.Id}_") &&
                                       kv.Key.Contains($"_r{classroom.Id}_"))
                            .Select(kv => kv.Value)
                            .ToList();

                        foreach (var variable in invalidVars)
                        {
                            // Constraint: The classroom with insufficient capacity cannot be assigned this course
                            model.Add(variable == 0);
                        }
                    }
                }
            }
        }

        private void AddPrerequisiteConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            // Create a mapping from course ID to section ID
            var courseToSections = new Dictionary<int, List<int>>();
            foreach (var section in problem.CourseSections)
            {
                if (!courseToSections.ContainsKey(section.CourseId))
                {
                    courseToSections[section.CourseId] = new List<int>();
                }
                courseToSections[section.CourseId].Add(section.Id);
            }

            // Process prerequisite constraints
            // Within the current semester, prerequisite courses cannot be scheduled in the same time slot as the subsequent courses
            foreach (var section in problem.CourseSections)
            {
                var course = section.Course;

                if (course?.Prerequisites == null || course.Prerequisites.Count == 0)
                    continue;

                foreach (var prerequisite in course.Prerequisites)
                {
                    // The CourseSection lists corresponding to the current course and prerequisite course
                    if (courseToSections.TryGetValue(course.CourseId, out var sectionIds) &&
                        courseToSections.TryGetValue(prerequisite.PrerequisiteCourseId, out var prereqSectionIds))
                    {
                        foreach (var timeSlot in problem.TimeSlots)
                        {
                            foreach (var sectionId in sectionIds)
                            {
                                foreach (var prereqSectionId in prereqSectionIds)
                                {
                                    var sectionTimeVars = _variables
                                        .Where(kv => kv.Key.StartsWith($"c{sectionId}_t{timeSlot.Id}_"))
                                        .Select(kv => kv.Value)
                                        .ToList();

                                    var prereqTimeVars = _variables
                                        .Where(kv => kv.Key.StartsWith($"c{prereqSectionId}_t{timeSlot.Id}_"))
                                        .Select(kv => kv.Value)
                                        .ToList();

                                    foreach (var sectionVar in sectionTimeVars)
                                    {
                                        foreach (var prereqVar in prereqTimeVars)
                                        {
                                            // Add: Prerequisite course and course cannot be scheduled at the same time
                                            model.Add(sectionVar + prereqVar <= 1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Calculate the capacity suitability of the classroom
        /// </summary>
        private int CalculateCapacityScore(int enrollment, int capacity)
        {
            if (capacity < enrollment)
            {
                // Insufficient capacity, not available
                return 0;
            }

            // Calculate the capacity utilization rate
            double utilizationRatio = (double)enrollment / capacity;

            if (utilizationRatio > 0.85)
            {
                // Utilization rate very high, close to full but not exceeding (optimal)
                return 5;
            }

            if (utilizationRatio > 0.7)
            {
                // High utilization rate
                return 4;
            }

            if (utilizationRatio > 0.5)
            {
                // Medium utilization rate
                return 3;
            }

            if (utilizationRatio > 0.3)
            {
                // Low utilization rate
                return 2;
            }

            // Low utilization rate, waste space
            return 1;
        }
    }
}