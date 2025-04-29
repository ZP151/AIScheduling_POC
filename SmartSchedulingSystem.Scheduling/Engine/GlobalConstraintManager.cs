using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Interfaces;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// Global constraint manager access point, used to share constraint manager between different components
    /// </summary>
    public static class GlobalConstraintManager
    {
        private static ConstraintManager _current;

        /// <summary>
        /// Get or set the current constraint manager instance
        /// </summary>
        public static IConstraintManager Current
        {
            get { return _current; }
            set { _current = value as ConstraintManager; }
        }

        /// <summary>
        /// Initialize the global constraint manager
        /// </summary>
        public static void Initialize(ConstraintManager constraintManager)
        {
            _current = constraintManager;
            
            // Set to minimum constraint level to ensure initial solution can be found
            _current?.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
        }

        /// <summary>
        /// Clear the current constraint manager instance
        /// </summary>
        public static void ClearCurrent()
        {
            Current = null;
        }
    }
} 