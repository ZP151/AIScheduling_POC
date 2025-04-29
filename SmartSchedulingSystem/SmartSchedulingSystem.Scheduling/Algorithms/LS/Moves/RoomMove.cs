using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// Move that changes the classroom of a course assignment
    /// </summary>
    public class RoomMove : IMove
    {
        private readonly int _assignmentId;
        private readonly int _newRoomId;

        public RoomMove(int assignmentId, int newRoomId)
        {
            _assignmentId = assignmentId;
            _newRoomId = newRoomId;
        }
        public int NewClassroomId => _newRoomId;
        public int AssignmentId => _assignmentId;

        /// <summary>
        /// Apply the move to a solution
        /// </summary>
        public SchedulingSolution Apply(SchedulingSolution solution)
        {
            // Create deep copy of solution
            var newSolution = solution.Clone();

            // Find assignment to modify
            var assignment = newSolution.Assignments.FirstOrDefault(a => a.Id == _assignmentId);
            if (assignment != null)
            {
                // Update classroom information
                var classroom = newSolution.Problem.Classrooms.FirstOrDefault(r => r.Id == _newRoomId);
                if (classroom != null)
                {
                    assignment.ClassroomId = _newRoomId;
                    assignment.Building = classroom.Building;
                    assignment.ClassroomName = classroom.Name;
                }
            }

            return newSolution;
        }

        /// <summary>
        /// Get move description
        /// </summary>
        public string GetDescription()
        {
            return $"Move assignment {_assignmentId} to classroom {_newRoomId}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignmentId };
        }
    }
}