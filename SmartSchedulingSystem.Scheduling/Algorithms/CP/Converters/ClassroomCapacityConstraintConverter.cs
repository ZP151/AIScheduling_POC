using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// Converting classroom capacity constraints to CP model constraints
    /// </summary>
    public class ClassroomCapacityConstraintConverter : ICPConstraintConverter
    {
        /// <summary>
        /// Get the constraint level of the constraint converter
        /// </summary>
        public Engine.ConstraintApplicationLevel ConstraintLevel => Engine.ConstraintApplicationLevel.Basic;

        private readonly Dictionary<int, int> _classroomCapacities;
        private readonly Dictionary<int, int> _expectedEnrollments;

        public ClassroomCapacityConstraintConverter(
            IClassroomCapacityProvider capacityProvider,
            Dictionary<int, int> expectedEnrollments)
        {
            if (capacityProvider == null) throw new ArgumentNullException(nameof(capacityProvider));
            _classroomCapacities = capacityProvider.GetCapacities(); 
            _expectedEnrollments = expectedEnrollments ?? throw new ArgumentNullException(nameof(expectedEnrollments));
        }

        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // Process each course section
            foreach (var section in problem.CourseSections)
            {
                // Get the expected number of students for the section
                if (!_expectedEnrollments.TryGetValue(section.Id, out int enrollment))
                {
                    enrollment = section.Enrollment; // Use the default number of students for the section
                }

                // Process each classroom
                foreach (var classroom in problem.Classrooms)
                {
                    // Get the classroom capacity
                    if (!_classroomCapacities.TryGetValue(classroom.Id, out int capacity))
                    {
                        capacity = classroom.Capacity; // Use the default capacity of the classroom
                    }

                    // Check if the capacity is sufficient
                    if (capacity < enrollment)
                    {
                        // Find all variables that assign this section to this classroom
                        var invalidVars = variables
                            .Where(kv => kv.Key.StartsWith($"c{section.Id}_") &&
                                       kv.Key.Contains($"_r{classroom.Id}_"))
                            .Select(kv => kv.Value)
                            .ToList();

                        // Add constraints: these variables must be 0 (prohibit the class from using the classroom)
                        foreach (var variable in invalidVars)
                        {
                            model.Add(variable == 0);
                        }
                    }
                }
            }
        }
    }
    public interface IClassroomCapacityProvider
    {
        Dictionary<int, int> GetCapacities();
    }
    public class TestClassroomCapacityProvider : IClassroomCapacityProvider
    {
        public Dictionary<int, int> GetCapacities() => new Dictionary<int, int>
        {
            [1] = 50,
            [2] = 40,
            [3] = 60,
            [4] = 35,
            [5] = 45,
            [6] = 55,
            [7] = 70,
            [8] = 30,
            [9] = 65,
            [10] = 50,
            [11] = 40,
            [12] = 60,
            [13] = 45,
            [14] = 50,
            [15] = 55
        };
    }
}