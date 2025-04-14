using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // 响应DTO
    public class ScheduleResultDto
    {
        public int ScheduleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public double Score { get; set; }
        public List<ScheduleItemDto> Items { get; set; }
        public List<string> Conflicts { get; set; }
       
        // 添加字段
        public string AlgorithmType { get; set; } // 使用的算法类型
        public long ExecutionTimeMs { get; set; } // 执行时间（毫秒）
        public int SemesterId { get; set; } // 学期ID
        public string SemesterName { get; set; } // 学期名称
        public int TotalAssignments { get; set; } // 总分配数量
        
        // 评估指标
        public Dictionary<string, double> Metrics { get; set; }
        
        // 统计信息
        public Dictionary<string, int> Statistics { get; set; } // 例如：各校区教室使用情况、教师工作量等
    }

    // 新增：表示多个排课方案的DTO
    public class ScheduleResultsDto
    {
        public List<ScheduleResultDto> Solutions { get; set; } = new List<ScheduleResultDto>();
        public DateTime GeneratedAt { get; set; }
        public int TotalSolutions { get; set; }
        public double BestScore { get; set; }
        public double AverageScore { get; set; }
        
        // 主方案ID
        public int? PrimaryScheduleId { get; set; }
        
        // 错误消息 - 用于在生成排课方案失败时返回错误信息
        public string ErrorMessage { get; set; }
        
        // 是否成功
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage) && Solutions.Any();
    }
}
