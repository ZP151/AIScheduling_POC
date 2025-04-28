using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // Response DTO
    public class ScheduleResultDto
    {
        public int ScheduleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public double Score { get; set; }
        public List<ScheduleItemDto> Items { get; set; }
        public List<string> Conflicts { get; set; }
       
        // Add fields
        public string AlgorithmType { get; set; } // The type of algorithm used
        public long ExecutionTimeMs { get; set; } // Execution time (milliseconds)
        public int SemesterId { get; set; } // Semester ID
        public string SemesterName { get; set; } // Semester name
        public int TotalAssignments { get; set; } // Total number of assignments
        
        // Evaluation metrics
        public Dictionary<string, double> Metrics { get; set; }
        
        // Statistics
        public Dictionary<string, int> Statistics { get; set; } // For example: classroom usage by campus, teacher workload, etc.
    }
}
