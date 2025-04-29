using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// Constraint conversion interface, used to convert Domain constraints to CP model constraints
    /// </summary>
    public interface ICPConstraintConverter
    {
        /// <summary>
        /// Get the constraint level corresponding to this constraint converter
        /// </summary>
        ConstraintApplicationLevel ConstraintLevel { get; }

        /// <summary>
        /// Add constraints to CP model
        /// </summary>
        /// <param name="model">CP model</param>
        /// <param name="variables">Variable dictionary</param>
        /// <param name="problem">Scheduling problem</param>
        void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem);
    }
}
