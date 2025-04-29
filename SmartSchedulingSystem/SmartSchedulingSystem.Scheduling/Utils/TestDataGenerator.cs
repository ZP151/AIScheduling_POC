using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Utils
{
    /// <summary>
    /// Tool class for generating test data
    /// </summary>
    public class TestDataGenerator
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// Generate test scheduling problem
        /// </summary>
        /// <param name="courseSectionCount">Number of course sections</param>
        /// <param name="teacherCount">Number of teachers</param>
        /// <param name="classroomCount">Number of classrooms</param>
        /// <param name="timeSlotCount">Number of time slots</param>
        /// <returns>Test scheduling problem</returns>
        public SchedulingProblem GenerateTestProblem(
            int courseSectionCount = 20,
            int teacherCount = 10,
            int classroomCount = 15,
            int timeSlotCount = 30) 
        {
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = $"Test Problem {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                SemesterId = 1,
                CourseSections = new List<CourseSectionInfo>(),
                Teachers = new List<TeacherInfo>(),
                Classrooms = new List<ClassroomInfo>(),
                TimeSlots = new List<TimeSlotInfo>(),
                TeacherCoursePreferences = new List<TeacherCoursePreference>(),
                TeacherAvailabilities = new List<TeacherAvailability>(),
                ClassroomAvailabilities = new List<ClassroomAvailability>(),
                Prerequisites = new List<CoursePrerequisite>()
            };

            // 1. Generate time slots - ensure enough time slots
            GenerateTimeSlots(problem, Math.Max(courseSectionCount, timeSlotCount));

            // 2. Generate classrooms - ensure capacity is sufficient
            GenerateClassrooms(problem, classroomCount);

            // 3. Generate teachers
            GenerateTeachers(problem, teacherCount);

            // 4. Generate course sections - ensure reasonable class size
            GenerateCourseSectionsRealistic(problem, courseSectionCount);

            // 5. Create more realistic teacher course preferences: each teacher teaches 1-3 courses, each course has 1-3 teachers
            CreateRealisticTeacherPreferences(problem);
            Console.WriteLine($"Generated {problem.TeacherCoursePreferences.Count} teacher course preferences");

            // 6. Add limited unavailable times
            AddLimitedUnavailableTimes(problem);
            Console.WriteLine($"Generated {problem.TeacherAvailabilities.Count} teacher unavailable times");
            Console.WriteLine($"Generated {problem.ClassroomAvailabilities.Count} classroom unavailable times");


            return problem;
        }
        public static List<TimeSlotInfo> GenerateStandardTimeSlots()
        {
            var timeSlots = new List<TimeSlotInfo>();
            int slotId = 1;

            // Monday to Friday
            for (int day = 1; day <= 5; day++) // Monday to Friday
            {
                // morning 8:00 - 10:00
                timeSlots.Add(new TimeSlotInfo
                {
                    Id = slotId++,
                    DayOfWeek = day,
                    DayName = GetDayName(day),
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0)
                });

                // morning 10:00 - 12:00
                timeSlots.Add(new TimeSlotInfo
                {
                    Id = slotId++,
                    DayOfWeek = day,
                    DayName = GetDayName(day),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0)
                });

                // afternoon 14:00 - 16:00
                timeSlots.Add(new TimeSlotInfo
                {
                    Id = slotId++,
                    DayOfWeek = day,
                    DayName = GetDayName(day),
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0)
                });

                // evening 19:00 - 21:00
                timeSlots.Add(new TimeSlotInfo
                {
                    Id = slotId++,
                    DayOfWeek = day,
                    DayName = GetDayName(day),
                    StartTime = new TimeSpan(19, 0, 0),
                    EndTime = new TimeSpan(21, 0, 0)
                });
            }        
            return timeSlots;
        }
        private static string GetDayName(int day)
        {
            return day switch
            {
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                7 => "Sunday",
                _ => "Unknown"
            };
        }
        /// <summary>
        /// Generate test time slots
        /// </summary>
        // Generate reasonable time slots
        private void GenerateTimeSlots(SchedulingProblem problem, int count)
        {
            // 5 time slots per day, 5 days a week
            int slotsPerDay = 5;
            int maxDays = 5;

            int slotId = 1;
            for (int day = 1; day <= maxDays && slotId <= count; day++)
            {
                for (int slot = 0; slot < slotsPerDay && slotId <= count; slot++)
                {
                    // 8:00-9:30, 10:00-11:30, 13:00-14:30, 15:00-16:30, 18:30-20:00
                    TimeSpan startTime;
                    TimeSpan endTime;

                    switch (slot)
                    {
                        case 0: // morning first slot
                            startTime = new TimeSpan(8, 0, 0);
                            endTime = new TimeSpan(9, 30, 0);
                            break;
                        case 1: // morning second slotng second slot
                            startTime = new TimeSpan(10, 0, 0);
                            endTime = new TimeSpan(11, 30, 0);
                            break;
                        case 2: // afternoon first slot
                            startTime = new TimeSpan(13, 0, 0);
                            endTime = new TimeSpan(14, 30, 0);
                            break;
                        case 3: // afternoon second slot
                            startTime = new TimeSpan(15, 0, 0);
                            endTime = new TimeSpan(16, 30, 0);
                            break;
                        case 4: // evening slot
                            startTime = new TimeSpan(18, 30, 0);
                            endTime = new TimeSpan(20, 0, 0);
                            break;
                        default:
                            startTime = new TimeSpan(8 + slot * 2, 0, 0);
                            endTime = new TimeSpan(9 + slot * 2, 30, 0);
                            break;
                    }

                    problem.TimeSlots.Add(new TimeSlotInfo
                    {
                        Id = slotId,
                        DayOfWeek = day,
                        DayName = GetDayName(day),
                        StartTime = startTime,
                        EndTime = endTime
                    });

                    slotId++;
                }
            }
        }


        /// <summary>
        /// Generate test classrooms
        /// </summary>
        private void GenerateClassrooms(SchedulingProblem problem, int count)
        {
            // different types, different capacities
            string[] types = { "Regular", "Computer", "Lab", "Lecture" };
            string[] buildings = { "A Building", "B Building", "C Building", "Experiment Building", "Library" };

            for (int i = 1; i <= count; i++)
            {
                string type = types[_random.Next(types.Length)];
                string building = buildings[_random.Next(buildings.Length)];

                // set reasonable capacity based on classroom type
                int capacity;
                bool hasComputers = false;
                bool hasProjector = _random.NextDouble() > 0.2; // 80% have projector

                switch (type)
                {
                    case "Regular":
                        capacity = _random.Next(30, 61); // 30-60 people
                        break;
                    case "Computer":
                        capacity = _random.Next(20, 41); // 20-40 people
                        hasComputers = true;
                        break;
                    case "Lab":
                        capacity = _random.Next(20, 36); // 20-35 people
                        break;
                    case "Lecture":
                        capacity = _random.Next(80, 201); // 80-200 people
                        break;
                    default:
                        capacity = _random.Next(30, 61);
                        break;
                }

                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"{building}-{100 + i}",
                    Building = building,
                    CampusId = _random.Next(1, 3), // 1 or 2, representing main campus or branch campus
                    CampusName = _random.Next(1, 3) == 1 ? "Main Campus" : "Branch Campus",
                    Capacity = capacity,
                    Type = type,
                    HasComputers = hasComputers,
                    HasProjector = hasProjector
                });
            }
        }

        /// <summary>
        /// Generate classroom equipment list
        /// </summary>
        private string GenerateEquipment(string roomType)
        {
            var equipment = new List<string>();

            // basic equipment
            if (_random.NextDouble() > 0.1) equipment.Add("Whiteboard");
            if (_random.NextDouble() > 0.2) equipment.Add("Projector");

            // add special equipment based on classroom type
            switch (roomType)
            {
                case "Computer":
                    equipment.Add("Computers");
                    if (_random.NextDouble() > 0.5) equipment.Add("Printer");
                    break;
                case "Lab":
                    equipment.Add("LabBench");
                    if (_random.NextDouble() > 0.5) equipment.Add("SafetyEquipment");
                    break;
                case "Lecture":
                    equipment.Add("Microphone");
                    if (_random.NextDouble() > 0.3) equipment.Add("SoundSystem");
                    break;
                case "Seminar":
                    if (_random.NextDouble() > 0.5) equipment.Add("RoundTable");
                    break;
            }

            return string.Join(",", equipment);
        }

        private void GenerateTeachers(SchedulingProblem problem, int count)
        {
            string[] surnames = { "Zhang", "Wang", "Li", "Zhao", "Liu", "Chen", "Yang", "Huang", "Zhou", "Wu", "Zheng", "Ma", "Sun", "Zhu", "Hu" };
            string[] titles = { "Professor", "Associate Professor", "Lecturer", "Assistant" };
            string[] departments = { "Computer Science", "Mathematics", "Physics", "Engineering", "Economics", "Management", "Foreign Languages" };

            for (int i = 1; i <= count; i++)
            {
                string surname = surnames[_random.Next(surnames.Length)];
                string title = titles[_random.Next(titles.Length)];
                int departmentId = _random.Next(1, departments.Length + 1);

                // set reasonable workload based on title
                int maxWeeklyHours, maxDailyHours, maxConsecutiveHours;

                switch (title)
                {
                    case "Professor":
                        maxWeeklyHours = _random.Next(10, 17); // 10-16 hours
                        maxDailyHours = 6;
                        maxConsecutiveHours = 4;
                        break;
                    case "Associate Professor":
                        maxWeeklyHours = _random.Next(12, 19); // 12-18 hours
                        maxDailyHours = 6;
                        maxConsecutiveHours = 4;
                        break;
                    case "Lecturer":
                        maxWeeklyHours = _random.Next(14, 21); // 14-20 hours
                        maxDailyHours = 8;
                        maxConsecutiveHours = 6;
                        break;
                    case "Assistant":
                        maxWeeklyHours = _random.Next(16, 23); // 16-22 hours
                        maxDailyHours = 8;
                        maxConsecutiveHours = 6;
                        break;
                    default:
                        maxWeeklyHours = _random.Next(12, 21); // 12-20 hours
                        maxDailyHours = 6;
                        maxConsecutiveHours = 4;
                        break;
                }

                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"{surname}{_random.Next(1, 10)}",
                    Title = title,
                    DepartmentId = departmentId,
                    DepartmentName = departments[departmentId - 1],
                    MaxWeeklyHours = maxWeeklyHours,
                    MaxDailyHours = maxDailyHours,
                    MaxConsecutiveHours = maxConsecutiveHours
                });
            }
        }

        // generate course sections, ensure class size is reasonable
        private void GenerateCourseSectionsRealistic(SchedulingProblem problem, int count)
        {
            string[] subjects = { "CS", "MATH", "PHYS", "ENG", "ECON", "MGT", "LANG" };
            string[] courseTypes = { "Regular", "Computer", "Lab", "Regular" }; // regular courses are more common

            Dictionary<string, string[]> subjectCourses = new Dictionary<string, string[]>
            {
                { "CS", new[] { "Programming", "Data Structures", "Algorithms", "Database", "Computer Networks", "Operating Systems", "Software Engineering", "Artificial Intelligence" } },
                { "MATH", new[] { "Advanced Mathematics", "Linear Algebra", "Probability Theory", "Discrete Mathematics", "Numerical Analysis", "Statistics", "Operations Research" } },
                { "PHYS", new[] { "Mechanics", "Electromagnetics", "Thermodynamics", "Optics", "Atomic Physics", "Quantum Mechanics", "Relativity" } },
                { "ENG", new[] { "Engineering Mechanics", "Material Mechanics", "Circuit Theory", "Signals and Systems", "Automatic Control", "Microelectronics" } },
                { "ECON", new[] { "Microeconomics", "Macroeconomics", "Econometrics", "Finance", "International Trade", "Public Finance" } },
                { "MGT", new[] { "Management Principles", "Marketing", "Human Resources", "Financial Management", "Strategic Management", "Operations Management" } },
                { "LANG", new[] { "English", "Japanese", "German", "French", "Russian", "Spanish", "Chinese" } }
            };

            // generate 1-3 classes for each course
            int sectionId = 1;
            int courseId = 1;

            while (sectionId <= count)
            {
                string subject = subjects[_random.Next(subjects.Length)];
                string[] courseNames = subjectCourses[subject];
                string courseName = courseNames[_random.Next(courseNames.Length)];
                string courseType = courseTypes[_random.Next(courseTypes.Length)];

                int courseNumber = 100 + courseId;
                string courseCode = $"{subject}{courseNumber}";

                // determine how many classes this course has
                int sectionCount = Math.Min(_random.Next(1, 4), count - sectionId + 1); // 1-3 classes, but not more than remaining count

                for (int section = 0; section < sectionCount; section++)
                {
                    char sectionChar = (char)('A' + section);

                    // set class size based on course type
                    int enrollment;
                    string requiredRoomType;

                    switch (courseType)
                    {
                        case "Regular":
                            enrollment = _random.Next(20, 61); // 20-60 people
                            requiredRoomType = "Regular";
                            break;
                        case "Computer":
                            enrollment = _random.Next(15, 31); // 15-30 people
                            requiredRoomType = "Computer";
                            break;
                        case "Lab":
                            enrollment = _random.Next(10, 26); // 10-25 people
                            requiredRoomType = "Lab";
                            break;
                        default:
                            enrollment = _random.Next(20, 51);
                            requiredRoomType = "Regular";
                            break;
                    }

                    problem.CourseSections.Add(new CourseSectionInfo
                    {
                        Id = sectionId,
                        CourseId = courseId,
                        CourseCode = courseCode,
                        CourseName = courseName,
                        SectionCode = $"{courseCode}-{sectionChar}",
                        Credits = 3,
                        Hours = 6,
                        Enrollment = enrollment,
                        DepartmentId = _random.Next(1, 5),
                        DepartmentName = "Department " + _random.Next(1, 5),
                        CourseType = courseType,
                        RequiredRoomType = requiredRoomType
                    });

                    sectionId++;
                    if (sectionId > count) break;
                }

                courseId++;
            }
        }

        private void CreateRealisticTeacherPreferences(SchedulingProblem problem)
        {
            // Clear existing preferences
            problem.TeacherCoursePreferences.Clear();

            // Get all distinct course IDs
            var distinctCourseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .ToList();

            // Assign 1-3 teachers for each course
            foreach (var courseId in distinctCourseIds)
            {
                // Get course information
                var course = problem.CourseSections.First(s => s.CourseId == courseId);

                // Determine how many teachers needed for this course
                int teacherCount = _random.Next(1, 4); // 1-3 teachers

                // Randomly select teachers
                var selectedTeachers = problem.Teachers
                    .OrderBy(x => _random.Next())
                    .Take(teacherCount)
                    .ToList();

                foreach (var teacher in selectedTeachers)
                {
                    // Add preference with high proficiency and preference values
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = courseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5, ensure qualified
                        PreferenceLevel = _random.Next(3, 6)   // 3-5, ensure high preference
                    });
                }
            }

            // Check if any teachers have no courses assigned
            var teachersWithoutCourses = problem.Teachers
                .Where(t => !problem.TeacherCoursePreferences.Any(p => p.TeacherId == t.Id))
                .ToList();

            // Assign 1-2 courses to teachers without any courses
            foreach (var teacher in teachersWithoutCourses)
            {
                int courseCount = _random.Next(1, 3); // 1-2 courses

                var coursesToAssign = distinctCourseIds
                    .OrderBy(x => _random.Next())
                    .Take(courseCount);

                foreach (var courseId in coursesToAssign)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = courseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5
                        PreferenceLevel = _random.Next(3, 6)   // 3-5
                    });
                }
            }

            // Check constraint: ensure each teacher teaches at most 3 different courses
            foreach (var teacher in problem.Teachers)
            {
                var teacherCourses = problem.TeacherCoursePreferences
                    .Where(p => p.TeacherId == teacher.Id)
                    .Select(p => p.CourseId)
                    .Distinct()
                    .ToList();

                if (teacherCourses.Count > 3)
                {
                    // If more than 3 courses, randomly remove excess courses
                    var coursesToKeep = teacherCourses
                        .OrderBy(x => _random.Next())
                        .Take(3)
                        .ToHashSet();

                    var prefsToRemove = problem.TeacherCoursePreferences
                        .Where(p => p.TeacherId == teacher.Id && !coursesToKeep.Contains(p.CourseId))
                        .ToList();

                    foreach (var pref in prefsToRemove)
                    {
                        problem.TeacherCoursePreferences.Remove(pref);
                    }
                }
            }
        }
        private void AddLimitedUnavailableTimes(SchedulingProblem problem)
        {
            // Clear existing availability settings
            problem.TeacherAvailabilities.Clear();
            problem.ClassroomAvailabilities.Clear();

            // 1. Add a few unavailable time slots for teachers
            foreach (var teacher in problem.Teachers)
            {
                // Each teacher has 1-2 unavailable time slots
                int unavailableCount = _random.Next(1, 3);

                var unavailableSlots = problem.TimeSlots
                    .OrderBy(x => _random.Next())
                    .Take(unavailableCount)
                    .ToList();

                foreach (var slot in unavailableSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = false,
                        PreferenceLevel = 0
                    });
                }
            }

            // 2. Add a few unavailable time slots for classrooms
            foreach (var classroom in problem.Classrooms)
            {
                // Each classroom has 0-1 unavailable time slots
                int unavailableCount = _random.Next(2) == 0 ? 1 : 0;

                if (unavailableCount > 0)
                {
                    var unavailableSlot = problem.TimeSlots
                        .OrderBy(x => _random.Next())
                        .First();

                    problem.ClassroomAvailabilities.Add(new ClassroomAvailability
                    {
                        ClassroomId = classroom.Id,
                        TimeSlotId = unavailableSlot.Id,
                        IsAvailable = false,
                        UnavailableReason = _random.Next(2) == 0 ? "Maintenance" : "Reserved"
                    });
                }
            }

            // Check: ensure we don't add too many constraints that make the problem unsolvable
            EnsureFeasibility(problem);
        }

        /// <summary>
        /// Ensure problem feasibility by avoiding too many constraints that could make it unsolvable
        /// </summary>
        private void EnsureFeasibility(SchedulingProblem problem)
        {
            // Simple check: ensure each course has at least one possible arrangement
            foreach (var section in problem.CourseSections)
            {
                // 1. Find qualified teachers for this course
                var qualifiedTeachers = problem.TeacherCoursePreferences
                    .Where(p => p.CourseId == section.CourseId)
                    .Select(p => p.TeacherId)
                    .ToList();

                if (qualifiedTeachers.Count == 0)
                {
                    // If no qualified teachers, randomly select one and add preference
                    var randomTeacher = problem.Teachers[_random.Next(problem.Teachers.Count)];

                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = randomTeacher.Id,
                        CourseId = section.CourseId,
                        ProficiencyLevel = 5,
                        PreferenceLevel = 4
                    });

                    qualifiedTeachers.Add(randomTeacher.Id);
                }

                // 2. Find suitable classrooms for this course
                var suitableRooms = problem.Classrooms
                    .Where(r => r.Capacity >= section.Enrollment &&
                               (string.IsNullOrEmpty(section.RequiredRoomType) ||
                                r.Type == section.RequiredRoomType ||
                                section.RequiredRoomType == "Regular"))
                    .Select(r => r.Id)
                    .ToList();

                if (suitableRooms.Count == 0)
                {
                    // If no suitable rooms, modify one to make it suitable
                    var largestRoom = problem.Classrooms
                        .OrderByDescending(r => r.Capacity)
                        .First();

                    largestRoom.Capacity = Math.Max(largestRoom.Capacity, section.Enrollment + 10);

                    if (!string.IsNullOrEmpty(section.RequiredRoomType) &&
                        section.RequiredRoomType != "Regular" &&
                        largestRoom.Type != section.RequiredRoomType)
                    {
                        largestRoom.Type = section.RequiredRoomType;
                    }

                    suitableRooms.Add(largestRoom.Id);
                }

                // 3. Ensure each qualified teacher has at least one available time slot in each suitable classroom
                foreach (var teacherId in qualifiedTeachers)
                {
                    var unavailableTimeSlots = problem.TeacherAvailabilities
                        .Where(a => a.TeacherId == teacherId && !a.IsAvailable)
                        .Select(a => a.TimeSlotId)
                        .ToHashSet();

                    if (unavailableTimeSlots.Count >= problem.TimeSlots.Count)
                    {
                        // If teacher has no available times, remove some unavailability constraints
                        var availabilitiesToRemove = problem.TeacherAvailabilities
                            .Where(a => a.TeacherId == teacherId)
                            .Take(problem.TeacherAvailabilities.Count - 3) // Ensure at least 3 time slots available
                            .ToList();

                        foreach (var a in availabilitiesToRemove)
                        {
                            problem.TeacherAvailabilities.Remove(a);
                        }
                    }
                }

                foreach (var roomId in suitableRooms)
                {
                    var unavailableTimeSlots = problem.ClassroomAvailabilities
                        .Where(a => a.ClassroomId == roomId && !a.IsAvailable)
                        .Select(a => a.TimeSlotId)
                        .ToHashSet();

                    if (unavailableTimeSlots.Count >= problem.TimeSlots.Count)
                    {
                        // If classroom has no available times, remove some unavailability constraints
                        var availabilitiesToRemove = problem.ClassroomAvailabilities
                            .Where(a => a.ClassroomId == roomId)
                            .Take(problem.ClassroomAvailabilities.Count - 3) // Ensure at least 3 time slots available
                            .ToList();

                        foreach (var a in availabilitiesToRemove)
                        {
                            problem.ClassroomAvailabilities.Remove(a);
                        }
                    }
                }
            }
        }

        private void GenerateTeacherAvailabilities(SchedulingProblem problem)
        {
            // By default, teachers are available at all time slots, only need to generate unavailable time slots
            foreach (var teacher in problem.Teachers)
            {
                // Randomly select 0-3 unavailable time slots
                int unavailableCount = _random.Next(4);
                var unavailableSlots = problem.TimeSlots
                    .OrderBy(x => _random.Next())
                    .Take(unavailableCount)
                    .ToList();

                foreach (var slot in unavailableSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = false,
                        PreferenceLevel = 0
                    });
                }

                // Randomly select 1-2 high preference time slots
                int preferredCount = _random.Next(1, 3);
                var preferredSlots = problem.TimeSlots
                    .Except(unavailableSlots)
                    .OrderBy(x => _random.Next())
                    .Take(preferredCount)
                    .ToList();

                foreach (var slot in preferredSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = true,
                        PreferenceLevel = 5 // Highest preference
                    });
                }
            }
        }
        // Ensure each course has at least one qualified teacher
        private void EnsureQualifiedTeachersForAllCourses(SchedulingProblem problem)
        {
            // Clear any existing teacher course preferences
            problem.TeacherCoursePreferences.Clear();

            // Get all distinct course IDs
            var distinctCourseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .ToList();

            // For each course, ensure at least two teachers are qualified to teach
            foreach (var courseId in distinctCourseIds)
            {
                // Select at least two teachers (or all if total teachers less than 2)
                int minTeachers = Math.Min(2, problem.Teachers.Count);
                int teacherCount = _random.Next(minTeachers, problem.Teachers.Count + 1);
                var selectedTeachers = problem.Teachers
                    .OrderBy(x => _random.Next())
                    .Take(teacherCount)
                    .ToList();

                foreach (var teacher in selectedTeachers)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = courseId,
                        ProficiencyLevel = _random.Next(3, 6), // 3-5, ensure qualified
                        PreferenceLevel = _random.Next(3, 6)   // 3-5, ensure high preference
                    });
                }
            }

            // Add some additional random preferences to increase test data diversity
            foreach (var teacher in problem.Teachers)
            {
                foreach (var courseId in distinctCourseIds)
                {
                    // Skip if combination already exists
                    if (problem.TeacherCoursePreferences.Any(p =>
                        p.TeacherId == teacher.Id && p.CourseId == courseId))
                    {
                        continue;
                    }

                    // Randomly add some additional preferences
                    if (_random.NextDouble() < 0.3) // 30% chance to add
                    {
                        problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                        {
                            TeacherId = teacher.Id,
                            CourseId = courseId,
                            ProficiencyLevel = _random.Next(1, 6), // 1-5, including less qualified
                            PreferenceLevel = _random.Next(1, 6)   // 1-5, including low preferences
                        });
                    }
                }
            }
        }
        // Limit the number of unavailable time slots to ensure each resource has enough available time
        private void LimitUnavailabilityPeriods(SchedulingProblem problem)
        {
            // Clear existing availability settings
            problem.TeacherAvailabilities.Clear();
            problem.ClassroomAvailabilities.Clear();

            // Calculate each resource can have maximum unavailable time slots
            int maxUnavailablePerTeacher = Math.Max(1, problem.TimeSlots.Count / 10);
            int maxUnavailablePerClassroom = Math.Max(1, problem.TimeSlots.Count / 20);

            // Add some unavailable time slots for teachers
            foreach (var teacher in problem.Teachers)
            {
                // Randomly select some unavailable time slots
                int unavailableCount = _random.Next(maxUnavailablePerTeacher + 1);
                var unavailableSlots = problem.TimeSlots
                    .OrderBy(x => _random.Next())
                    .Take(unavailableCount)
                    .ToList();

                foreach (var slot in unavailableSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = false,
                        PreferenceLevel = 0
                    });
                }

                // Randomly add some high preference time slots
                int preferredCount = _random.Next(1, 4); // 1-3 high preference time slots
                var preferredSlots = problem.TimeSlots
                    .Except(unavailableSlots)
                    .OrderBy(x => _random.Next())
                    .Take(preferredCount)
                    .ToList();

                foreach (var slot in preferredSlots)
                {
                    problem.TeacherAvailabilities.Add(new TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = true,
                        PreferenceLevel = 5 // Highest preference
                    });
                }
            }

            // Add some unavailable time slots for classrooms
            foreach (var classroom in problem.Classrooms)
            {
                // Randomly select some unavailable time slots
                int unavailableCount = _random.Next(maxUnavailablePerClassroom + 1);
                var unavailableSlots = problem.TimeSlots
                    .OrderBy(x => _random.Next())
                    .Take(unavailableCount)
                    .ToList();

                foreach (var slot in unavailableSlots)
                {
                    problem.ClassroomAvailabilities.Add(new ClassroomAvailability
                    {
                        ClassroomId = classroom.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = false,
                        UnavailableReason = "Maintenance"
                    });
                }
            }
        }

        // Generate safe prerequisite relationships (avoid mutual dependencies within the same semester)
        private void GenerateSafePrerequisites(SchedulingProblem problem)
        {
            // For simplicity, do not add prerequisite course constraints in test data
            problem.Prerequisites.Clear();

            // If you need to add some simple prerequisite relationships, you can use the following method
            // But ensure these relationships do not cause circular dependencies
            /*
            var courseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            if (courseIds.Count >= 3)
            {
                // Only add prerequisites for third and subsequent courses
                // And ensure prerequisite course ID is always less than current course
                for (int i = 2; i < Math.Min(5, courseIds.Count); i++)
                {
                    int courseId = courseIds[i];
                    int prereqId = courseIds[i - 2]; // Ensure enough distance to avoid conflicts

                    problem.Prerequisites.Add(new CoursePrerequisite
                    {
                        CourseId = courseId,
                        PrerequisiteCourseId = prereqId
                    });
                }
            }
            */
        }
        private void GenerateClassroomAvailabilities(SchedulingProblem problem)
        {
            // By default, classrooms are available at all time slots, only need to generate unavailable time slots
            foreach (var classroom in problem.Classrooms)
            {   
                // Randomly select 0-2 unavailable time slots
                int unavailableCount = _random.Next(3);
                var unavailableSlots = problem.TimeSlots
                    .OrderBy(x => _random.Next())
                    .Take(unavailableCount)
                    .ToList();

                foreach (var slot in unavailableSlots)
                {
                    problem.ClassroomAvailabilities.Add(new ClassroomAvailability
                    {
                        ClassroomId = classroom.Id,
                        TimeSlotId = slot.Id,
                        IsAvailable = false,
                        UnavailableReason = "Maintenance"
                    });
                }
            }
        }

        private void GeneratePrerequisites(SchedulingProblem problem)
        {
            // Get all distinct course IDs
            var courseIds = problem.CourseSections
                .Select(s => s.CourseId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            // Only add prerequisite relationships when there are more than one course
            if (courseIds.Count > 1)
            {
                // Randomly generate 1-3 prerequisite relationships
                int prereqCount = Math.Min(_random.Next(1, 4), courseIds.Count - 1);

                for (int i = 0; i < prereqCount; i++)
                {
                    // Randomly select a subsequent course
                    int laterCourseIndex = _random.Next(1, courseIds.Count);
                    int laterCourseId = courseIds[laterCourseIndex];

                    // Select a prerequisite course (must be ID less than current course)
                    int prereqCourseIndex = _random.Next(laterCourseIndex);
                    int prereqCourseId = courseIds[prereqCourseIndex];

                    // Add prerequisite relationship
                    problem.Prerequisites.Add(new CoursePrerequisite
                    {
                        CourseId = laterCourseId,
                        PrerequisiteCourseId = prereqCourseId
                    });
                }
            }
        }

        private void GenerateConstraints(SchedulingProblem problem)
        {
            // In actual use, constraints are provided through dependency injection, this is just an example
            // Implementation located in DependencyInjection.cs in registered
        }

       

        // Used to generate test solutions
        public SchedulingSolution CreateTestSolution(SchedulingProblem problem = null)
        {
            problem ??= GenerateTestProblem();

            var solution = new SchedulingSolution
            {
                Id = 1,
                ProblemId = problem.Id,
                Problem = problem,
                Name = "Test Solution",
                CreatedAt = DateTime.Now,
                Algorithm = "Test",
                Assignments = new List<SchedulingAssignment>()
            };

            // Create random assignments for each course section
            int assignmentId = 1;
            foreach (var section in problem.CourseSections)
            {
                // Randomly select time slot, classroom, and teacher
                var timeSlot = problem.TimeSlots.OrderBy(x => _random.Next()).FirstOrDefault();
                var classroom = problem.Classrooms.OrderBy(x => _random.Next()).FirstOrDefault();

                // Select qualified teachers for this course
                var qualifiedTeachers = problem.TeacherCoursePreferences
                    .Where(p => p.CourseId == section.CourseId)
                    .Select(p => p.TeacherId)
                    .ToList();

                var teacher = qualifiedTeachers.Count > 0
                    ? problem.Teachers.FirstOrDefault(t => qualifiedTeachers.Contains(t.Id))
                    : problem.Teachers.OrderBy(x => _random.Next()).FirstOrDefault();

                if (timeSlot != null && classroom != null && teacher != null)
                {
                    solution.Assignments.Add(new SchedulingAssignment
                    {
                        Id = assignmentId++,
                        SectionId = section.Id,
                        SectionCode = section.SectionCode,
                        ClassroomId = classroom.Id,
                        ClassroomName = classroom.Name,
                        TeacherId = teacher.Id,
                        TeacherName = teacher.Name,
                        TimeSlotId = timeSlot.Id,
                        DayOfWeek = timeSlot.DayOfWeek,
                        StartTime = timeSlot.StartTime,
                        EndTime = timeSlot.EndTime,
                        WeekPattern = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
                    });
                }
            }

            return solution;
        }

        public SchedulingProblem CreateSimpleValidProblem()
        {
            // Create basic problem structure
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Simple Valid Test Problem",
                SemesterId = 1,
                CourseSections = new List<CourseSectionInfo>(),
                Teachers = new List<TeacherInfo>(),
                Classrooms = new List<ClassroomInfo>(),
                TimeSlots = new List<TimeSlotInfo>(),
                TeacherCoursePreferences = new List<TeacherCoursePreference>(),
                TeacherAvailabilities = new List<TeacherAvailability>(),
                ClassroomAvailabilities = new List<ClassroomAvailability>(),
                Prerequisites = new List<CoursePrerequisite>()
            };

            // Create time slots (3 days × 2 periods = 6 time slots)
            for (int day = 1; day <= 3; day++)
            {
                for (int slot = 0; slot < 2; slot++)
                {
                    var startTime = new TimeSpan(8 + 2 * slot, 0, 0);
                    var endTime = new TimeSpan(9 + 2 * slot, 30, 0);

                    problem.TimeSlots.Add(new TimeSlotInfo
                    {
                        Id = (day - 1) * 2 + slot + 1,
                        DayOfWeek = day,
                        DayName = $"Day {day}",
                        StartTime = startTime,
                        EndTime = endTime
                    });
                }
            }

            // Create classrooms (2 classrooms)
            for (int i = 1; i <= 2; i++)
            {
                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"Room {i}",
                    Building = "Main Building",
                    CampusId = 1,
                    CampusName = "Main Campus",
                    Capacity = 30 + (i - 1) * 10, // First classroom capacity 30 people, second classroom capacity 40 people
                    Type = "Regular",
                    HasComputers = false,
                    HasProjector = true
                });
            }

            // Create teachers (2 teachers)
            for (int i = 1; i <= 2; i++)
            {
                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"Teacher {i}",
                    Title = "Professor",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    MaxWeeklyHours = 20,
                    MaxDailyHours = 6,
                    MaxConsecutiveHours = 3
                });
            }

            // Create course sections (3 course sections)
            int[] enrollments = { 25, 35, 20 };
            for (int i = 1; i <= 3; i++)
            {
                problem.CourseSections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = i,
                    CourseCode = $"CS10{i}",
                    CourseName = $"Course {i}",
                    SectionCode = $"CS10{i}-A",
                    Credits = 3,
                    Hours = 6,
                    Enrollment = enrollments[i - 1],
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    CourseType = "Regular",
                    RequiredRoomType = "Regular"
                });
            }

            // Add teacher course preferences (ensure each teacher can teach all courses)
            foreach (var teacher in problem.Teachers)
            {
                foreach (var course in problem.CourseSections)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = course.CourseId,
                        ProficiencyLevel = 5, // High ability value
                        PreferenceLevel = 4   // High preference
                    });
                }
            }

            // Ensure all teachers are available at all times
            foreach (var teacher in problem.Teachers)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // No need to explicitly add available record, default to be available
                }
            }

            // Ensure all classrooms are available at all times
            foreach (var classroom in problem.Classrooms)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // No need to explicitly add available record, default to be available
                }
            }

            return problem;
        }
        // Add to TestDataGenerator class
        public SchedulingProblem CreateGuaranteedFeasibleProblem()
        {
            // Create basic problem structure
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Guaranteed Feasible Problem",
                SemesterId = 1,
                CourseSections = new List<CourseSectionInfo>(),
                Teachers = new List<TeacherInfo>(),
                Classrooms = new List<ClassroomInfo>(),
                TimeSlots = new List<TimeSlotInfo>(),
                TeacherCoursePreferences = new List<TeacherCoursePreference>(),
                TeacherAvailabilities = new List<TeacherAvailability>(),
                ClassroomAvailabilities = new List<ClassroomAvailability>(),
                Prerequisites = new List<CoursePrerequisite>(),
                Constraints = new List<IConstraint>() // Initialize as empty to avoid NullReferenceException
            };

            // Create 1 time slot
            problem.TimeSlots.Add(new TimeSlotInfo
            {
                Id = 1,
                DayOfWeek = 1,
                DayName = "Monday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(9, 30, 0)
            });

            // Create 1 classroom
            problem.Classrooms.Add(new ClassroomInfo
            {
                Id = 1,
                Name = "Room 101",
                Building = "Building A",
                CampusId = 1,
                CampusName = "Main Campus",
                Capacity = 100, // Ensure enough large
                Type = "Regular",
                HasComputers = true,
                HasProjector = true
            });

            // Create 1 teacher
            problem.Teachers.Add(new TeacherInfo
            {
                Id = 1,
                Name = "Professor Smith",
                Title = "Professor",
                DepartmentId = 1,
                DepartmentName = "Computer Science",
                MaxWeeklyHours = 40,
                MaxDailyHours = 8,
                MaxConsecutiveHours = 4
            });

            // Create 1 course section
            problem.CourseSections.Add(new CourseSectionInfo
            {
                Id = 1,
                CourseId = 1,
                CourseCode = "CS101",
                CourseName = "Introduction to Programming",
                SectionCode = "CS101-A",
                Credits = 3,
                Hours = 6,
                Enrollment = 50,
                DepartmentId = 1,
                DepartmentName = "Computer Science",
                CourseType = "Regular",
                RequiredRoomType = "Regular"
            });

            // Add teacher course preferences
            problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
            {
                TeacherId = 1,
                CourseId = 1,
                ProficiencyLevel = 5, // Highest ability value
                PreferenceLevel = 5   // Highest preference
            });

            return problem;
        }
        public SchedulingProblem CreateDebugFeasibleProblem()
        {
            // Create basic problem structure
            var problem = new SchedulingProblem
            {
                Id = 1,
                Name = "Debug Feasible Problem",
                SemesterId = 1,
                CourseSections = new List<CourseSectionInfo>(),
                Teachers = new List<TeacherInfo>(),
                Classrooms = new List<ClassroomInfo>(),
                TimeSlots = new List<TimeSlotInfo>(),
                TeacherCoursePreferences = new List<TeacherCoursePreference>(),
                TeacherAvailabilities = new List<TeacherAvailability>(),
                ClassroomAvailabilities = new List<ClassroomAvailability>(),
                Prerequisites = new List<CoursePrerequisite>(),
                Constraints = new List<IConstraint>()
            };

            // Create 3 time slots (1 per day)
            for (int day = 1; day <= 3; day++)
            {
                problem.TimeSlots.Add(new TimeSlotInfo
                {
                    Id = day,
                    DayOfWeek = day,
                    DayName = $"Day {day}",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0)
                });
            }

            // Create 3 large enough classrooms 
            for (int i = 1; i <= 3; i++)
            {
                problem.Classrooms.Add(new ClassroomInfo
                {
                    Id = i,
                    Name = $"Room {i}",
                    Building = "Main Building",
                    CampusId = 1,
                    CampusName = "Main Campus",
                    Capacity = 100, // Ensure enough large capacity
                    Type = "Regular",
                    HasComputers = true,
                    HasProjector = true
                });
            }

            // Create 3 teachers
            for (int i = 1; i <= 3; i++)
            {
                problem.Teachers.Add(new TeacherInfo
                {
                    Id = i,
                    Name = $"Teacher {i}",
                    Title = "Professor",
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    MaxWeeklyHours = 40,
                    MaxDailyHours = 8,
                    MaxConsecutiveHours = 4
                });
            }

            // Create 3 course sections, ensure course ID and section ID are consistent (simplified debugging)
            for (int i = 1; i <= 3; i++)
            {
                problem.CourseSections.Add(new CourseSectionInfo
                {
                    Id = i,
                    CourseId = i,
                    CourseCode = $"CS10{i}",
                    CourseName = $"Course {i}",
                    SectionCode = $"CS10{i}-A",
                    Credits = 3,
                    Hours = 6,
                    Enrollment = 30, // Below all classroom capacity
                    DepartmentId = 1,
                    DepartmentName = "Computer Science",
                    CourseType = "Regular",
                    RequiredRoomType = "Regular"
                });
            }

            // Add teacher course preferences - Each teacher can teach all courses, ability value and preference are highest
            foreach (var teacher in problem.Teachers)
            {
                foreach (var course in problem.CourseSections)
                {
                    problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                    {
                        TeacherId = teacher.Id,
                        CourseId = course.CourseId,
                        ProficiencyLevel = 5, // Highest ability value
                        PreferenceLevel = 5   // Highest preference
                    });
                }
            }

            // Ensure no unavailable teacher time slots
            // (No TeacherAvailability record added, default to be available at all times)

            // Ensure no unavailable classroom time slots
            // (No ClassroomAvailability record added, default to be available at all times)

            // No prerequisite relationship added to avoid unnecessary complexity

            return problem;
        }
    }
}