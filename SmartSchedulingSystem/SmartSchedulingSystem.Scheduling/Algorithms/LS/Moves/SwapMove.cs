using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// Move that swaps attributes between two course assignments
    /// </summary>
    public class SwapMove : IMove
    {
        private readonly int _assignment1Id;
        private readonly int _assignment2Id;
        private readonly bool _swapTime;
        private readonly bool _swapRoom;
        private readonly bool _swapTeacher;

        /// <summary>
        /// Create a new swap move
        /// </summary>
        /// <param name="assignment1Id">First assignment ID</param>
        /// <param name="assignment2Id">Second assignment ID</param>
        /// <param name="swapTime">Whether to swap time</param>
        /// <param name="swapRoom">Whether to swap room</param>
        /// <param name="swapTeacher">Whether to swap teacher</param>
        public SwapMove(int assignment1Id, int assignment2Id, bool swapTime, bool swapRoom, bool swapTeacher)
        {
            _assignment1Id = assignment1Id;
            _assignment2Id = assignment2Id;
            _swapTime = swapTime;
            _swapRoom = swapRoom;
            _swapTeacher = swapTeacher;
        }
        public int AssignmentId1 => _assignment1Id;
        public int AssignmentId2 => _assignment2Id;
        public bool SwapTime => _swapTime;
        public bool SwapRoom => _swapRoom;
        public bool SwapTeacher => _swapTeacher;

        /// <summary>
        /// Apply the move to a solution
        /// </summary>
        public SchedulingSolution Apply(SchedulingSolution solution)
        {
            var newSolution = solution.Clone();
            var assignment1 = newSolution.Assignments.FirstOrDefault(a => a.Id == _assignment1Id);
            var assignment2 = newSolution.Assignments.FirstOrDefault(a => a.Id == _assignment2Id);

            if (assignment1 != null && assignment2 != null)
            {
                // Swap time slots if requested
                if (_swapTime)
                {
                    var tempTimeSlotId = assignment1.TimeSlotId;
                    var tempDayOfWeek = assignment1.DayOfWeek;
                    var tempStartTime = assignment1.StartTime;
                    var tempEndTime = assignment1.EndTime;

                    assignment1.TimeSlotId = assignment2.TimeSlotId;
                    assignment1.DayOfWeek = assignment2.DayOfWeek;
                    assignment1.StartTime = assignment2.StartTime;
                    assignment1.EndTime = assignment2.EndTime;

                    assignment2.TimeSlotId = tempTimeSlotId;
                    assignment2.DayOfWeek = tempDayOfWeek;
                    assignment2.StartTime = tempStartTime;
                    assignment2.EndTime = tempEndTime;
                }

                // Swap rooms if requested
                if (_swapRoom)
                {
                    var tempRoomId = assignment1.ClassroomId;
                    var tempBuilding = assignment1.Building;
                    var tempClassroomName = assignment1.ClassroomName;

                    assignment1.ClassroomId = assignment2.ClassroomId;
                    assignment1.Building = assignment2.Building;
                    assignment1.ClassroomName = assignment2.ClassroomName;

                    assignment2.ClassroomId = tempRoomId;
                    assignment2.Building = tempBuilding;
                    assignment2.ClassroomName = tempClassroomName;
                }

                // Swap teachers if requested
                if (_swapTeacher)
                {
                    var tempTeacherId = assignment1.TeacherId;
                    assignment1.TeacherId = assignment2.TeacherId;
                    assignment2.TeacherId = tempTeacherId;
                }
            }

            return newSolution;
        }

        /// <summary>
        /// Get move description
        /// </summary>
        public string GetDescription()
        {
            var swapTypes = new List<string>();
            if (_swapTime) swapTypes.Add("time");
            if (_swapRoom) swapTypes.Add("room");
            if (_swapTeacher) swapTypes.Add("teacher");

            return $"Swap {string.Join(", ", swapTypes)} between assignments {_assignment1Id} and {_assignment2Id}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignment1Id, _assignment2Id };
        }
    }
}