using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.AlgorithmsImpl
{
    public static class FieldReplacer
    {
        public static List<SchedulingAssignment> GenerateFeasibleAlternatives(
            SchedulingAssignment original,
            SchedulingProblem problem,
            SchedulingSolution solution,
            IConstraintManager constraintManager)
        {
            var alternatives = new List<SchedulingAssignment>();

            foreach (var timeSlot in problem.TimeSlots)
                foreach (var teacher in problem.Teachers)
                    foreach (var classroom in problem.Classrooms)
                    {
                        var candidate = new SchedulingAssignment
                        {
                            CourseSectionId = original.CourseSectionId,
                            TimeSlot = timeSlot,
                            Teacher = teacher,
                            Classroom = classroom
                        };

                        if (constraintManager.IsValidReplacement(solution, original, candidate))
                        {
                            alternatives.Add(candidate);
                        }
                    }

            return alternatives;
        }
    }

}
