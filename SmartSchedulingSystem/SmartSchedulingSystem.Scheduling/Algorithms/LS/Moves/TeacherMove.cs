using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// Move that changes the teacher of a course assignment
    /// </summary>
    public class TeacherMove : IMove
    {
        private readonly int _assignmentId;
        private readonly int _newTeacherId;

        public TeacherMove(int assignmentId, int newTeacherId)
        {
            _assignmentId = assignmentId;
            _newTeacherId = newTeacherId;
        }
        public int NewTeacherId => _newTeacherId;
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
                // Update teacher
                assignment.TeacherId = _newTeacherId;
            }

            return newSolution;
        }

        /// <summary>
        /// Get move description
        /// </summary>
        public string GetDescription()
        {
            return $"Change teacher of assignment {_assignmentId} to {_newTeacherId}";
        }

        public int[] GetAffectedAssignmentIds()
        {
            return new[] { _assignmentId };
        }
    }
}