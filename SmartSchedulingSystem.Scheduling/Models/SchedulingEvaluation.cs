using System.Collections.Generic;
using System.Data;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// 排课解决方案的评估结果
    /// </summary>
    public class SchedulingEvaluation
    {
        /// <summary>
        /// 解决方案是否可行（满足所有硬约束）
        /// </summary>
        public bool IsFeasible { get; set; }

        /// <summary>
        /// 总评分（0-1，1为最佳）
        /// </summary>
        public double Score { get; set; }
        /// <summary>
        /// 解决方案ID
        /// </summary>
        public int SolutionId { get; set; }

        /// <summary>
        /// 硬约束是否都满足
        /// </summary>
        public bool HardConstraintsSatisfied { get; set; }

        /// <summary>
        /// 硬约束满足程度（0-1）
        /// </summary>
        public double HardConstraintsSatisfactionLevel { get; set; }

        /// <summary>
        /// 软约束满足程度（0-1）
        /// </summary>
        public double SoftConstraintsSatisfactionLevel { get; set; }
        /// <summary>
        /// 硬约束评估
        /// </summary>
        public List<ConstraintEvaluation> HardConstraintEvaluations { get; set; } = new List<ConstraintEvaluation>();

        /// <summary>
        /// 软约束评估
        /// </summary>
        public List<ConstraintEvaluation> SoftConstraintEvaluations { get; set; } = new List<ConstraintEvaluation>();

        /// <summary>
        /// 检测到的冲突
        /// </summary>
        public List<SchedulingConflict> Conflicts { get; set; } = new List<SchedulingConflict>();
    }

    /// <summary>
    /// 单个约束的评估结果
    /// </summary>
    public class ConstraintEvaluation
    {
        /// <summary>
        /// 被评估的约束
        /// </summary>
        public IConstraint Constraint { get; set; }

        /// <summary>
        /// 约束ID
        /// </summary>
        public int ConstraintId => Constraint?.Id ?? 0;

        /// <summary>
        /// 约束名称
        /// </summary>
        public string ConstraintName => Constraint?.Name ?? string.Empty;

        /// <summary>
        /// 是否是硬约束
        /// </summary>
        public bool IsHard => Constraint?.IsHard ?? false;

        /// <summary>
        /// 约束权重
        /// </summary>
        public double Weight => Constraint?.Weight ?? 1.0;

        /// <summary>
        /// 约束是否满足
        /// </summary>
        public bool Satisfied => Score >= 0.99; // 当得分接近1时认为约束满足

        /// <summary>
        /// 约束满足程度（0-1，1为完全满足）
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 约束相关的冲突
        /// </summary>
        public List<SchedulingConflict> Conflicts { get; set; } = new List<SchedulingConflict>();
    }

    /// <summary>
    /// 冲突解决选项
    /// </summary>
    public class ConflictResolutionOption
    {
        /// <summary>
        /// 选项ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 冲突ID
        /// </summary>
        public int ConflictId { get; set; }

        /// <summary>
        /// 选项描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 兼容性评分（0-100，100为最佳）
        /// </summary>
        public int Compatibility { get; set; }

        /// <summary>
        /// 潜在影响
        /// </summary>
        public List<string> Impacts { get; set; } = new List<string>();

        /// <summary>
        /// 应用此选项所需的操作
        /// </summary>
        public List<ResolutionAction> Actions { get; set; } = new List<ResolutionAction>();

        /// <summary>
        /// 将此解决方案应用到排课方案
        /// </summary>
        /// <param name="solution">排课方案</param>
        public void Apply(SchedulingSolution solution)
        {
            foreach (var action in Actions)
            {
                action.Execute(solution);
            }
        }
    }

    /// <summary>
    /// 解决方案操作
    /// </summary>
    public abstract class ResolutionAction
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public ResolutionActionType Type { get; set; }

        /// <summary>
        /// 执行操作
        /// </summary>
        /// <param name="solution">排课方案</param>
        public abstract void Execute(SchedulingSolution solution);
    }

    /// <summary>
    /// 重新分配教师操作
    /// </summary>
    public class ReassignTeacherAction : ResolutionAction
    {
        /// <summary>
        /// 排课分配ID
        /// </summary>
        public int AssignmentId { get; set; }

        /// <summary>
        /// 新教师ID
        /// </summary>
        public int NewTeacherId { get; set; }

        /// <summary>
        /// 新教师名称
        /// </summary>
        public string NewTeacherName { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ReassignTeacherAction()
        {
            Type = ResolutionActionType.ReassignTeacher;
        }

        /// <summary>
        /// 执行操作
        /// </summary>
        /// <param name="solution">排课方案</param>
        public override void Execute(SchedulingSolution solution)
        {
            var assignment = solution.Assignments.FirstOrDefault(a => a.Id == AssignmentId);
            if (assignment != null)
            {
                assignment.TeacherId = NewTeacherId;
                assignment.TeacherName = NewTeacherName;
            }
        }
    }

    public class ReassignClassroomAction : ResolutionAction
    {
        public int AssignmentId { get; set; }
        public int NewClassroomId { get; set; }
        public string NewClassroomName { get; set; }

        public ReassignClassroomAction()
        {
            Type = ResolutionActionType.ReassignClassroom;
        }

        public override void Execute(SchedulingSolution solution)
        {
            var assignment = solution.Assignments.FirstOrDefault(a => a.Id == AssignmentId);
            if (assignment != null)
            {
                assignment.ClassroomId = NewClassroomId;
                assignment.ClassroomName = NewClassroomName;
            }
        }
    }

    public class ReassignTimeSlotAction : ResolutionAction
    {
        public int AssignmentId { get; set; }
        public int NewTimeSlotId { get; set; }
        public int NewDayOfWeek { get; set; }
        public TimeSpan NewStartTime { get; set; }
        public TimeSpan NewEndTime { get; set; }

        public ReassignTimeSlotAction()
        {
            Type = ResolutionActionType.ReassignTimeSlot;
        }

        public override void Execute(SchedulingSolution solution)
        {
            var assignment = solution.Assignments.FirstOrDefault(a => a.Id == AssignmentId);
            if (assignment != null)
            {
                assignment.TimeSlotId = NewTimeSlotId;
               
            }
        }
    }

    public class RemoveAssignmentAction : ResolutionAction
    {
        public int AssignmentId { get; set; }

        public RemoveAssignmentAction()
        {
            Type = ResolutionActionType.RemoveAssignment;
        }

        public override void Execute(SchedulingSolution solution)
        {
            solution.RemoveAssignment(AssignmentId);
        }
    }

    public class AddAssignmentAction : ResolutionAction
    {
        public SchedulingAssignment NewAssignment { get; set; }

        public AddAssignmentAction()
        {
            Type = ResolutionActionType.AddAssignment;
        }

        public override void Execute(SchedulingSolution solution)
        {
            solution.AddAssignment(NewAssignment);
        }
    }

    public enum ResolutionActionType
    {
        ReassignTeacher,
        ReassignClassroom,
        ReassignTimeSlot,
        RemoveAssignment,
        AddAssignment,
        Other
    }
}