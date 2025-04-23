using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level1_CoreHard
{
    /// <summary>
    /// 教室冲突约束：确保同一时间段一个教室只能安排一门课程
    /// 核心硬约束 - Level1_CoreHard
    /// </summary>
    public class ClassroomConflictConstraint : BaseConstraint
    {
        public override int Id => 2;
        public override string Name => "Classroom Conflict Avoidance";
        public override string Description => "Ensures a classroom is not assigned to multiple courses in the same time slot";
        public override bool IsHard => true;
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_CoreHard;
        public override string Category => "Resource Conflicts";
        public override string DefinitionId => ConstraintDefinitions.ClassroomConflict;
        public override string BasicRule => BasicSchedulingRules.ResourceConflictAvoidance;

        public ClassroomConflictConstraint()
        {
            IsActive = true;
            Weight = 1.0;
        }

        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            
            // 检测教室冲突
            var roomTimeSlots = new Dictionary<(int roomId, int timeSlotId), List<SchedulingAssignment>>();
            
            foreach (var assignment in solution.Assignments)
            {
                var key = (assignment.ClassroomId, assignment.TimeSlotId);
                
                if (!roomTimeSlots.ContainsKey(key))
                {
                    roomTimeSlots[key] = new List<SchedulingAssignment>();
                }
                
                roomTimeSlots[key].Add(assignment);
            }
            
            // 添加所有冲突
            foreach (var item in roomTimeSlots)
            {
                if (item.Value.Count > 1)
                {
                    var (roomId, timeSlotId) = item.Key;
                    var conflictingSections = item.Value.Select(a => a.SectionId).ToList();
                    
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = Id,
                        Type = SchedulingConflictType.ClassroomConflict,
                        Description = $"Classroom (ID: {roomId}) is assigned to multiple courses at the same time slot",
                        Severity = ConflictSeverity.Critical,
                        InvolvedEntities = new Dictionary<string, List<int>>
                        {
                            { "Classrooms", new List<int> { roomId } },
                            { "Sections", conflictingSections }
                        },
                        InvolvedTimeSlots = new List<int> { timeSlotId }
                    });
                }
            }
            
            // 如果没有冲突，得分为1，否则为0（硬约束）
            double score = conflicts.Count == 0 ? 1.0 : 0.0;
            
            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            var (score, _) = Evaluate(solution);
            return score >= 1.0;
        }
    }
} 