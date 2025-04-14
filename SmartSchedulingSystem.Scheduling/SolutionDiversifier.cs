using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling
{
    public class SolutionDiversifier
    {
        private readonly Random _random;

        public SolutionDiversifier(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public SchedulingSolution Diversify(SchedulingSolution solution, double diversityFactor)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (diversityFactor < 0 || diversityFactor > 1)
                throw new ArgumentException("多样性因子必须在0到1之间", nameof(diversityFactor));

            var newSolution = solution.Clone();
            var assignments = newSolution.Assignments;
            int changesToMake = (int)(assignments.Count * diversityFactor);

            for (int i = 0; i < changesToMake; i++)
            {
                int index = _random.Next(assignments.Count);
                var assignment = assignments[index];

                // 随机选择一个可用的时间槽
                var availableTimeSlots = GetAvailableTimeSlots(newSolution, assignment);
                if (!availableTimeSlots.Any())
                    continue;

                int randomIndex = _random.Next(availableTimeSlots.Count);
                var newTimeSlot = availableTimeSlots[randomIndex];

                // 更新时间槽
                assignment.TimeSlotId = newTimeSlot.FirstTimeSlotId;
            }

            return newSolution;
        }

        private List<(int FirstTimeSlotId, int? SecondTimeSlotId)> GetAvailableTimeSlots(
            SchedulingSolution solution, 
            SchedulingAssignment assignment)
        {
            var availableSlots = new List<(int FirstTimeSlotId, int? SecondTimeSlotId)>();
            var timeSlots = solution.Problem.TimeSlots;

            foreach (var firstSlot in timeSlots)
            {
                // 检查教师在此时间段是否可用
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && 
                                        ta.TimeSlotId == firstSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // 创建临时分配来检查冲突
                var tempAssignment = new SchedulingAssignment
                {
                    Id = assignment.Id,
                    TeacherId = assignment.TeacherId,
                    ClassroomId = assignment.ClassroomId,
                    TimeSlotId = firstSlot.Id,
                };

               
                
            }

            return availableSlots;
        }
    }
} 