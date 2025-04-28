using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves
{
    /// <summary>
    /// Interface for all types of moves in local search
    /// </summary>
    public interface IMove
    {
        /// <summary>
        /// Apply the move to a solution
        /// </summary>
        /// <param name="solution">Current solution</param>
        /// <returns>New solution after applying the move</returns>
        SchedulingSolution Apply(SchedulingSolution solution);

        /// <summary>
        /// Get move description
        /// </summary>
        /// <returns>Description of the move</returns>
        string GetDescription();

        /// <summary>
        /// Get IDs of assignments affected by this move
        /// </summary>
        /// <returns>Array of affected assignment IDs</returns>
        int[] GetAffectedAssignmentIds();
    }
}