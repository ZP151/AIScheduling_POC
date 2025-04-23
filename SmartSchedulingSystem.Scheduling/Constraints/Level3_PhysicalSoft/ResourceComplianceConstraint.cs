using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level3_PhysicalSoft
{
    /// <summary>
    /// 资源兼容性约束 - 合并物理资源匹配相关约束
    /// </summary>
    public class ResourceComplianceConstraint : BaseConstraint, IConstraint
    {
        /// <summary>
        /// 功能开关
        /// </summary>
        private readonly bool _enableClassroomTypeMatch;
        private readonly bool _enableEquipmentRequirement;
        private readonly bool _enableLocationProximity;

        /// <summary>
        /// 约束定义ID
        /// </summary>
        public override string DefinitionId => ConstraintDefinitions.ClassroomTypeMatch;

        /// <summary>
        /// 基本规则
        /// </summary>
        public override string BasicRule => BasicSchedulingRules.ResourcePreference;
        
        /// <summary>
        /// ID
        /// </summary>
        public override int Id => 9;
        
        /// <summary>
        /// 名称
        /// </summary>
        public override string Name { get; } = "资源兼容性约束";
        
        /// <summary>
        /// 描述
        /// </summary>
        public override string Description { get; } = "确保课程被安排到合适教室并考虑位置临近";
        
        /// <summary>
        /// 是否是硬约束
        /// </summary>
        public override bool IsHard => false;
        
        /// <summary>
        /// 约束层级
        /// </summary>
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level3_PhysicalSoft;
        
        /// <summary>
        /// 约束类别
        /// </summary>
        public override string Category => "ResourceAllocation";

        /// <summary>
        /// 构造函数
        /// </summary>
        public ResourceComplianceConstraint() : this(true, true, true)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ResourceComplianceConstraint(
            bool enableClassroomTypeMatch,
            bool enableEquipmentRequirement,
            bool enableLocationProximity)
        {
            Weight = 0.5;
            
            _enableClassroomTypeMatch = enableClassroomTypeMatch;
            _enableEquipmentRequirement = enableEquipmentRequirement;
            _enableLocationProximity = enableLocationProximity;
        }

        /// <summary>
        /// 评估约束
        /// </summary>
        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (!IsValidSolution(solution))
                return (1.0, new List<SchedulingConflict>());

            var subScores = new List<(double Score, double Weight)>();
            var allConflicts = new List<SchedulingConflict>();
            
            // 评估教室类型匹配
            if (_enableClassroomTypeMatch)
            {
                var (score, conflicts) = EvaluateClassroomTypeMatch(solution);
                subScores.Add((score, 0.4));
                allConflicts.AddRange(conflicts);
            }
            
            // 评估设备需求匹配
            if (_enableEquipmentRequirement)
            {
                var (score, conflicts) = EvaluateEquipmentRequirement(solution);
                subScores.Add((score, 0.4));
                allConflicts.AddRange(conflicts);
            }
            
            // 评估位置临近约束
            if (_enableLocationProximity)
            {
                var (score, conflicts) = EvaluateLocationProximity(solution);
                subScores.Add((score, 0.2));
                allConflicts.AddRange(conflicts);
            }

            // 计算加权平均分
            double finalScore = 1.0;
            if (subScores.Any())
            {
                double totalWeight = subScores.Sum(s => s.Weight);
                finalScore = subScores.Sum(s => s.Score * s.Weight) / totalWeight;
            }

            return (finalScore, allConflicts);
        }

        private bool IsValidSolution(SchedulingSolution solution)
        {
            return solution != null && 
                   solution.Assignments != null && 
                   solution.Assignments.Count > 0 && 
                   solution.Problem != null;
        }

        private (double Score, List<SchedulingConflict> Conflicts) EvaluateClassroomTypeMatch(SchedulingSolution solution)
        {
            List<SchedulingConflict> conflicts = new List<SchedulingConflict>();
            int validAssignments = 0;
            int mismatchCount = 0;
            
            foreach (var assignment in solution.Assignments)
            {
                if (assignment.ClassroomId <= 0)
                    continue;

                validAssignments++;
                var course = solution.Problem.CourseSections.FirstOrDefault(c => c.Id == assignment.CourseSectionId);
                var classroom = solution.Problem.Classrooms.FirstOrDefault(cr => cr.Id == assignment.ClassroomId);

                if (course != null && classroom != null && 
                    !string.IsNullOrEmpty(course.RequiredClassroomType) && 
                    classroom.ClassroomType != course.RequiredClassroomType)
                {
                    mismatchCount++;
                    conflicts.Add(CreateClassroomTypeMismatchConflict(solution, course, classroom));
                }
            }

            double score = validAssignments > 0 ? Math.Max(0, 1.0 - ((double)mismatchCount / validAssignments)) : 1.0;
            return (score, conflicts);
        }

        private SchedulingConflict CreateClassroomTypeMismatchConflict(SchedulingSolution solution, CourseSectionInfo course, ClassroomInfo classroom)
        {
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.ClassroomTypeMismatch,
                Description = $"课程 {course.CourseName} 需要类型为 {course.RequiredClassroomType} 的教室，但被分配到类型为 {classroom.Type} 的教室",
                Severity = ConflictSeverity.Minor,
                Category = "教室类型不匹配",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Courses", new List<int> { course.Id } },
                    { "Classrooms", new List<int> { classroom.Id } }
                }
            };
        }

        private (double Score, List<SchedulingConflict> Conflicts) EvaluateEquipmentRequirement(SchedulingSolution solution)
        {
            List<SchedulingConflict> conflicts = new List<SchedulingConflict>();
            int totalWithEquipmentRequirements = 0;
            int mismatchCount = 0;
            
            foreach (var assignment in solution.Assignments)
            {
                if (assignment.ClassroomId <= 0)
                    continue;

                var course = solution.Problem.CourseSections.FirstOrDefault(c => c.Id == assignment.CourseSectionId);
                var classroom = solution.Problem.Classrooms.FirstOrDefault(cr => cr.Id == assignment.ClassroomId);

                if (course != null && classroom != null && 
                    !string.IsNullOrEmpty(course.RequiredEquipment))
                {
                    totalWithEquipmentRequirements++;
                    
                    // 将逗号分隔的设备列表转换为List<string>
                    var requiredEquipment = course.RequiredEquipment
                        .Split(',')
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToList();
                    
                    var missingEquipment = requiredEquipment
                        .Where(eq => string.IsNullOrEmpty(classroom.Equipment) || !classroom.Equipment.Contains(eq))
                        .ToList();

                    if (missingEquipment.Count > 0)
                    {
                        mismatchCount++;
                        conflicts.Add(CreateEquipmentMismatchConflict(solution, course, classroom, missingEquipment));
                    }
                }
            }

            double score = totalWithEquipmentRequirements > 0 ? 
                Math.Max(0, 1.0 - ((double)mismatchCount / totalWithEquipmentRequirements)) : 1.0;
            return (score, conflicts);
        }

        private SchedulingConflict CreateEquipmentMismatchConflict(
            SchedulingSolution solution, CourseSectionInfo course, ClassroomInfo classroom, List<string> missingEquipment)
        {
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.ClassroomTypeMismatch,
                Description = $"课程 {course.CourseName} 需要设备 {string.Join(", ", missingEquipment)}，但教室中缺少这些设备",
                Severity = ConflictSeverity.Minor,
                Category = "设备需求不满足",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Courses", new List<int> { course.Id } },
                    { "Classrooms", new List<int> { classroom.Id } }
                }
            };
        }

        private (double Score, List<SchedulingConflict> Conflicts) EvaluateLocationProximity(SchedulingSolution solution)
        {
            List<SchedulingConflict> conflicts = new List<SchedulingConflict>();
            int totalConsecutive = 0;
            int distantCount = 0;
            
            // 按教师和日期分组
            var teacherDayGroups = solution.Assignments
                .Where(a => a.TeacherId > 0 && a.ClassroomId > 0)
                .GroupBy(a => new { a.TeacherId, DayOfWeek = solution.Problem.TimeSlots
                    .FirstOrDefault(ts => ts.Id == a.TimeSlotId)?.DayOfWeek ?? 0 })
                .ToList();

            foreach (var group in teacherDayGroups)
            {
                // 对组内课程按开始时间排序
                var sortedAssignments = group.OrderBy(a => 
                    solution.Problem.TimeSlots.FirstOrDefault(ts => ts.Id == a.TimeSlotId)?.StartTime 
                    ?? TimeSpan.Zero).ToList();
                
                // 检查连续课程
                for (int i = 0; i < sortedAssignments.Count - 1; i++)
                {
                    var current = sortedAssignments[i];
                    var next = sortedAssignments[i + 1];
                    
                    var currentSlot = solution.Problem.TimeSlots.FirstOrDefault(ts => ts.Id == current.TimeSlotId);
                    var nextSlot = solution.Problem.TimeSlots.FirstOrDefault(ts => ts.Id == next.TimeSlotId);
                    
                    if (currentSlot != null && nextSlot != null && IsConsecutive(currentSlot, nextSlot))
                    {
                        totalConsecutive++;
                        
                        var currentRoom = solution.Problem.Classrooms.FirstOrDefault(c => c.Id == current.ClassroomId);
                        var nextRoom = solution.Problem.Classrooms.FirstOrDefault(c => c.Id == next.ClassroomId);
                        
                        if (currentRoom != null && nextRoom != null && currentRoom.Building != nextRoom.Building)
                        {
                            distantCount++;
                            conflicts.Add(CreateDistanceConflict(solution, group.Key.TeacherId, current, next, currentSlot, currentRoom, nextRoom));
                        }
                    }
                }
            }

            double score = totalConsecutive > 0 ? 
                Math.Max(0, 1.0 - ((double)distantCount / totalConsecutive)) : 1.0;
            return (score, conflicts);
        }

        private SchedulingConflict CreateDistanceConflict(
            SchedulingSolution solution, int teacherId, 
            SchedulingAssignment current, SchedulingAssignment next,
            TimeSlotInfo timeSlot, ClassroomInfo currentRoom, ClassroomInfo nextRoom)
        {
            var teacher = solution.Problem.Teachers.FirstOrDefault(t => t.Id == teacherId);
            var teacherName = teacher?.Name ?? $"教师ID {teacherId}";
            
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.BuildingProximityConflict,
                Description = $"{teacherName} 在 {timeSlot.DayOfWeek} 日有连续课程，但教室位于不同建筑物",
                Severity = ConflictSeverity.Minor,
                Category = "建筑物距离过远",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Teachers", new List<int> { teacherId } },
                    { "Classrooms", new List<int> { current.ClassroomId, next.ClassroomId } }
                },
                InvolvedTimeSlots = new List<int> { current.TimeSlotId, next.TimeSlotId }
            };
        }

        private bool IsConsecutive(TimeSlotInfo first, TimeSlotInfo second)
        {
            return first.EndTime == second.StartTime;
        }
    }
} 