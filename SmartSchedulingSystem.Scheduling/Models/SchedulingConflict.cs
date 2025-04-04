using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Models
{
    public class SchedulingConflict
    {
        public int Id { get; set; }
        public int ConstraintId { get; set; }
        public SchedulingConflictType Type { get; set; }
        public string Description { get; set; }
        public ConflictSeverity Severity { get; set; }
        public Dictionary<string, List<int>> InvolvedEntities { get; set; } = new Dictionary<string, List<int>>();
        public List<int> InvolvedTimeSlots { get; set; } = new List<int>();

        // AI辅助分析字段
        public string RootCause { get; set; }
        public List<ConflictResolutionOption> ResolutionOptions { get; set; } = new List<ConflictResolutionOption>();
        public string AIAnalysisDetails { get; set; }
        public DateTime AnalyzedAt { get; set; }

        // 冲突分类
        public string Category { get; set; }

        // 冲突解决状态
        public ConflictResolutionStatus ResolutionStatus { get; set; } = ConflictResolutionStatus.Unresolved;

        // 应用的解决方案ID
        public int? AppliedResolutionId { get; set; }
    }

    public enum ConflictResolutionStatus
    {
        Unresolved,
        Resolved,
        Ignored,
        InProgress
    }

    /// <summary>
    /// 冲突类型
    /// </summary>
    public enum SchedulingConflictType
    {
        /// <summary>
        /// 教师冲突（同一时间被安排两门课）
        /// </summary>
        TeacherConflict,

        /// <summary>
        /// 教室冲突（同一时间有两门课）
        /// </summary>
        ClassroomConflict,

        /// <summary>
        /// 学生冲突（同一学生在同一时间有两门课）
        /// </summary>
        StudentConflict,

        /// <summary>
        /// 教师可用性冲突（教师在安排的时间不可用）
        /// </summary>
        TeacherAvailabilityConflict,

        /// <summary>
        /// 教室可用性冲突（教室在安排的时间不可用）
        /// </summary>
        ClassroomAvailabilityConflict,

        /// <summary>
        /// 教室容量不足
        /// </summary>
        ClassroomCapacityExceeded,

        /// <summary>
        /// 教室类型不匹配
        /// </summary>
        ClassroomTypeMismatch,

        /// <summary>
        /// 性别限制冲突
        /// </summary>
        GenderRestrictionConflict,

        /// <summary>
        /// 校区间旅行时间冲突
        /// </summary>
        CampusTravelTimeConflict,

        /// <summary>
        /// 课程先决条件冲突
        /// </summary>
        PrerequisiteConflict,

        /// <summary>
        /// 课程连续性冲突
        /// </summary>
        CourseSequenceConflict,

        /// <summary>
        /// 教师工作量超额
        /// </summary>
        TeacherWorkloadExceeded,

        /// <summary>
        /// 假期冲突
        /// </summary>
        HolidayConflict,

        /// <summary>
        /// 约束评估错误
        /// </summary>
        ConstraintEvaluationError,

        BuildingProximityConflict,
        /// <summary>
        /// 其他冲突
        /// </summary>
        Other
    }

    /// <summary>
    /// 冲突严重程度
    /// </summary>
    public enum ConflictSeverity
    {
        /// <summary>
        /// 轻微（可忽略）
        /// </summary>
        Minor,

        /// <summary>
        /// 中等
        /// </summary>
        Moderate,

        /// <summary>
        /// 严重
        /// </summary>
        Severe,

        /// <summary>
        /// 严重（需要立即解决）
        /// </summary>
        Critical
    }

}
