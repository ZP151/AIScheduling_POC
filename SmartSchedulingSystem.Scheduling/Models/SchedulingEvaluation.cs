using System.Collections.Generic;
using System.Data;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// Evaluation results of a scheduling solution
    /// </summary>
    public class SchedulingEvaluation
    {
        /// <summary>
        /// Whether the solution is feasible (satisfies all hard constraints)
        /// </summary>
        public bool IsFeasible { get; set; }

        /// <summary>
        /// Total score (0-1, 1 being best)
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Solution ID
        /// </summary>
        public int SolutionId { get; set; }

        /// <summary>
        /// Whether all hard constraints are satisfied
        /// </summary>
        public bool HardConstraintsSatisfied { get; set; }

        /// <summary>
        /// Hard constraints satisfaction level (0-1)
        /// </summary>
        public double HardConstraintsSatisfactionLevel { get; set; }

        /// <summary>
        /// Soft constraints satisfaction level (0-1)
        /// </summary>
        public double SoftConstraintsSatisfactionLevel { get; set; }

        /// <summary>
        /// Hard constraints evaluation
        /// </summary>
        public List<ConstraintEvaluation> HardConstraintEvaluations { get; set; } = new List<ConstraintEvaluation>();

        /// <summary>
        /// Soft constraints evaluation
        /// </summary>
        public List<ConstraintEvaluation> SoftConstraintEvaluations { get; set; } = new List<ConstraintEvaluation>();

        /// <summary>
        /// Detected conflicts
        /// </summary>
        public List<SchedulingConflict> Conflicts { get; set; } = new List<SchedulingConflict>();
    }

    /// <summary>
    /// Evaluation result of a single constraint
    /// </summary>
    public class ConstraintEvaluation
    {
        /// <summary>
        /// Constraint being evaluated
        /// </summary>
        public IConstraint Constraint { get; set; }

        /// <summary>
        /// Constraint ID
        /// </summary>
        public int ConstraintId => Constraint?.Id ?? 0;

        /// <summary>
        /// Constraint name
        /// </summary>
        public string ConstraintName => Constraint?.Name ?? string.Empty;

        /// <summary>
        /// Whether it is a hard constraint
        /// </summary>
        public bool IsHard => Constraint?.IsHard ?? false;

        /// <summary>
        /// Constraint weight
        /// </summary>
        public double Weight => Constraint?.Weight ?? 1.0;

        /// <summary>
        /// Whether the constraint is satisfied
        /// </summary>
        public bool Satisfied => Score >= 0.99; // Considered satisfied when score is close to 1

        /// <summary>
        /// Constraint satisfaction level (0-1, 1 being fully satisfied)
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Conflicts related to this constraint
        /// </summary>
        public List<SchedulingConflict> Conflicts { get; set; } = new List<SchedulingConflict>();
    }

    /// <summary>
    /// Conflict resolution option
    /// </summary>
    public class ConflictResolutionOption
    {
        /// <summary>
        /// Option ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Conflict ID
        /// </summary>
        public int ConflictId { get; set; }

        /// <summary>
        /// Option description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Compatibility score (0-100, 100 being best)
        /// </summary>
        public int Compatibility { get; set; }

        /// <summary>
        /// Potential impacts
        /// </summary>
        public List<string> Impacts { get; set; } = new List<string>();

        /// <summary>
        /// Actions required to apply this option
        /// </summary>
        public List<ResolutionAction> Actions { get; set; } = new List<ResolutionAction>();

        /// <summary>
        /// Apply this resolution to the scheduling solution
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        public void Apply(SchedulingSolution solution)
        {
            foreach (var action in Actions)
            {
                action.Execute(solution);
            }
        }
    }

    /// <summary>
    /// Resolution action
    /// </summary>
    public abstract class ResolutionAction
    {
        /// <summary>
        /// Action type
        /// </summary>
        public ResolutionActionType Type { get; set; }

        /// <summary>
        /// Execute the action
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        public abstract void Execute(SchedulingSolution solution);
    }

    /// <summary>
    /// Reassign teacher action
    /// </summary>
    public class ReassignTeacherAction : ResolutionAction
    {
        /// <summary>
        /// Assignment ID
        /// </summary>
        public int AssignmentId { get; set; }

        /// <summary>
        /// New teacher ID
        /// </summary>
        public int NewTeacherId { get; set; }

        /// <summary>
        /// New teacher name
        /// </summary>
        public string NewTeacherName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReassignTeacherAction()
        {
            Type = ResolutionActionType.ReassignTeacher;
        }

        /// <summary>
        /// Execute the action
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
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
                assignment.DayOfWeek = NewDayOfWeek;
                assignment.StartTime = NewStartTime;
                assignment.EndTime = NewEndTime;
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