using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// Move that changes the time slot of a course assignment
    /// </summary>
    public class TimeMove : IMove
    {
        private readonly int _assignmentId;
        private readonly int _newTimeSlotId;

        public TimeMove(int assignmentId, int newTimeSlotId)
        {
            _assignmentId = assignmentId;
            _newTimeSlotId = newTimeSlotId;
        }
        public int NewTimeSlotId => _newTimeSlotId;
        public int AssignmentId => _assignmentId;
        public SchedulingSolution Apply(SchedulingSolution solution)
        {
            var newSolution = solution.Clone();
            var assignment = newSolution.Assignments.FirstOrDefault(a => a.Id == _assignmentId);

            if (assignment != null)
            {
                // Update time slot information
                var timeSlot = newSolution.Problem.TimeSlots.FirstOrDefault(t => t.Id == _newTimeSlotId);
                if (timeSlot != null)
                {
                    assignment.TimeSlotId = _newTimeSlotId;
                    assignment.DayOfWeek = timeSlot.DayOfWeek;
                    assignment.StartTime = timeSlot.StartTime;
                    assignment.EndTime = timeSlot.EndTime;
                }
            }

            return newSolution;
        }

        /// <summary>
        /// Get move description
        /// </summary>
        public string GetDescription()
        {
            return $"Move assignment {_assignmentId} to time slot {_newTimeSlotId}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignmentId };
        }
    }
}