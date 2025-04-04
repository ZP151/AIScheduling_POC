using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// 排课结果，包含解决方案和评估信息
    /// </summary>
    public class SchedulingResult
    {
        /// <summary>
        /// 排课问题
        /// </summary>
        public SchedulingProblem Problem { get; set; }

        /// <summary>
        /// 最终解决方案
        /// </summary>
        public List<SchedulingSolution> Solutions { get; set; }

        public int SolutionSetId { get; set; }
        /// <summary>
        /// 解决方案评估
        /// </summary>
        public SchedulingEvaluation Evaluation { get; set; }

        /// <summary>
        /// 初始解决方案
        /// </summary>
        public SchedulingSolution InitialSolution { get; set; }

        /// <summary>
        /// 优化后的解决方案（可能与最终解决方案不同，如果进行了冲突解决）
        /// </summary>
        public SchedulingSolution OptimizedSolution { get; set; }

        /// <summary>
        /// 运行状态
        /// </summary>
        public SchedulingStatus Status { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Message { get; set; }
        /// <summary>
        /// 结果是否成功
        /// </summary>
        public bool IsSuccessful => Status == SchedulingStatus.Success;

        /// <summary>
        /// 统计信息
        /// </summary>
        public SchedulingStatistics Statistics { get; set; } = new SchedulingStatistics();

        /// <summary>
        /// 计算各种统计信息
        /// </summary>
        
    }

    /// <summary>
    /// 排课运行状态
    /// </summary>
    public enum SchedulingStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        NotStarted,

        /// <summary>
        /// 已生成初始解
        /// </summary>
        InitialSolutionGenerated,

        /// <summary>
        /// 成功（所有约束都满足）
        /// </summary>
        Success,

        /// <summary>
        /// 部分成功（软约束可能未完全满足）
        /// </summary>
        PartialSuccess,

        /// <summary>
        /// 失败（未能找到可行解）
        /// </summary>
        Failure,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled,

        /// <summary>
        /// 出错
        /// </summary>
        Error
    }

    /// <summary>
    /// 排课统计信息
    /// </summary>
    public class SchedulingStatistics
    {
        /// <summary>
        /// 总课程班级数
        /// </summary>
        public int TotalSections { get; set; }

        /// <summary>
        /// 已安排的课程班级数
        /// </summary>
        public int ScheduledSections { get; set; }

        /// <summary>
        /// 未安排的课程班级数
        /// </summary>
        public int UnscheduledSections { get; set; }

        /// <summary>
        /// 总教师数
        /// </summary>
        public int TotalTeachers { get; set; }

        /// <summary>
        /// 已分配的教师数
        /// </summary>
        public int AssignedTeachers { get; set; }

        /// <summary>
        /// 总教室数
        /// </summary>
        public int TotalClassrooms { get; set; }

        /// <summary>
        /// 已使用的教室数
        /// </summary>
        public int UsedClassrooms { get; set; }

        /// <summary>
        /// 教室利用信息
        /// </summary>
        public Dictionary<int, ClassroomUtilizationInfo> ClassroomUtilization { get; set; } = new Dictionary<int, ClassroomUtilizationInfo>();

        /// <summary>
        /// 平均教室利用率
        /// </summary>
        public double AverageClassroomUtilization { get; set; }

        /// <summary>
        /// 教师工作量信息
        /// </summary>
        public Dictionary<int, TeacherWorkloadInfo> TeacherWorkloads { get; set; } = new Dictionary<int, TeacherWorkloadInfo>();

        /// <summary>
        /// 教师工作量标准差（衡量工作量平衡性）
        /// </summary>
        public double TeacherWorkloadStdDev { get; set; }

        /// <summary>
        /// 时间槽利用信息
        /// </summary>
        public Dictionary<int, TimeSlotUtilizationInfo> TimeSlotUtilization { get; set; } = new Dictionary<int, TimeSlotUtilizationInfo>();

        /// <summary>
        /// 平均时间槽利用率
        /// </summary>
        public double AverageTimeSlotUtilization { get; set; }

        /// <summary>
        /// 高峰时段ID
        /// </summary>
        public int PeakTimeSlotId { get; set; }

        /// <summary>
        /// 高峰时段利用率
        /// </summary>
        public double PeakTimeSlotUtilization { get; set; }

        /// <summary>
        /// 低谷时段ID
        /// </summary>
        public int LowestTimeSlotId { get; set; }

        /// <summary>
        /// 低谷时段利用率
        /// </summary>
        public double LowestTimeSlotUtilization { get; set; }
    }

    /// <summary>
    /// 教室利用信息
    /// </summary>
    public class ClassroomUtilizationInfo
    {
        /// <summary>
        /// 教室ID
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// 教室名称
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// 所在建筑物
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// 利用率
        /// </summary>
        public double UtilizationRate { get; set; }

        /// <summary>
        /// 安排的课程数
        /// </summary>
        public int AssignmentCount { get; set; }
    }

    /// <summary>
    /// 教师工作量信息
    /// </summary>
    public class TeacherWorkloadInfo
    {
        /// <summary>
        /// 教师ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// 教师名称
        /// </summary>
        public string TeacherName { get; set; }

        /// <summary>
        /// 总学时
        /// </summary>
        public int TotalHours { get; set; }

        /// <summary>
        /// 每日工作量（学时）
        /// </summary>
        public Dictionary<int, int> DailyWorkload { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// 最高日工作量（学时）
        /// </summary>
        public int MaxDailyHours { get; set; }

        /// <summary>
        /// 最大连续课时数
        /// </summary>
        public int MaxConsecutiveHours { get; set; }

        /// <summary>
        /// 安排的课程数
        /// </summary>
        public int AssignmentCount { get; set; }
    }

    /// <summary>
    /// 时间槽利用信息
    /// </summary>
    public class TimeSlotUtilizationInfo
    {
        /// <summary>
        /// 时间槽ID
        /// </summary>
        public int TimeSlotId { get; set; }

        /// <summary>
        /// 星期几（1-7）
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// 利用率
        /// </summary>
        public double UtilizationRate { get; set; }

        /// <summary>
        /// 安排的课程数
        /// </summary>
        public int AssignmentCount { get; set; }
    }
}