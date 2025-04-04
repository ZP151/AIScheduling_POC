using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Models
{
    public class SchedulingResultSet
    {
        /// <summary>
        /// 排课问题
        /// </summary>
        public SchedulingProblem Problem { get; set; }

        /// <summary>
        /// 多个解决方案（含主解）
        /// </summary>
        public SchedulingSolutionSet SolutionSet { get; set; }

        /// <summary>
        /// 每个方案的评估结果
        /// </summary>
        public Dictionary<int, SchedulingEvaluation> Evaluations { get; set; } = new();

        /// <summary>
        /// 结果集级别的统计信息（可汇总多个方案）
        /// </summary>
        public SchedulingStatistics SummaryStatistics { get; set; } = new();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 状态（如是否成功、失败、多解是否可用等）
        /// </summary>
        public SchedulingStatus Status { get; set; }

        /// <summary>
        /// 使用的参数
        /// </summary>
        public SchedulingParameters ParametersUsed { get; set; }

        /// <summary>
        /// 是否成功（主方案成功）
        /// </summary>
        public bool IsSuccessful => Status == SchedulingStatus.Success;
    }

}
