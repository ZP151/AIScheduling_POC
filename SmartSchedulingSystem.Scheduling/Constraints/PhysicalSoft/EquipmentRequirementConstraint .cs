// 5. 设备需求约束 - 软约束
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Soft
{
    public class EquipmentRequirementConstraint : IConstraint
    {
        private readonly Dictionary<int, List<string>> _sectionRequiredEquipment; // 班级ID -> 所需设备列表
        private readonly Dictionary<int, List<string>> _classroomEquipment; // 教室ID -> 提供的设备列表

        public int Id { get; } = 8;
        public string Name { get; } = "Equipment Requirements";
        public string Description { get; } = "Ensures classrooms have the required equipment for courses";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.8;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level2_PhysicalSoft;
        public string Category => "Physical Resources";

        public EquipmentRequirementConstraint(
            Dictionary<int, List<string>> sectionRequiredEquipment,
            Dictionary<int, List<string>> classroomEquipment)
        {
            _sectionRequiredEquipment = sectionRequiredEquipment ?? throw new ArgumentNullException(nameof(sectionRequiredEquipment));
            _classroomEquipment = classroomEquipment ?? throw new ArgumentNullException(nameof(classroomEquipment));
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            int totalRequirements = 0;
            int satisfiedRequirements = 0;

            foreach (var assignment in solution.Assignments)
            {
                // 检查课程是否有设备要求
                if (_sectionRequiredEquipment.TryGetValue(assignment.SectionId, out List<string> requiredEquipment) &&
                    requiredEquipment.Count > 0)
                {
                    totalRequirements++;

                    // 获取教室提供的设备
                    bool hasEquipment = _classroomEquipment.TryGetValue(assignment.ClassroomId, out List<string> availableEquipment);

                    if (hasEquipment)
                    {
                        // 检查是否所有所需设备都可用
                        bool allRequirementsMet = requiredEquipment.All(req => availableEquipment.Contains(req));

                        if (allRequirementsMet)
                        {
                            satisfiedRequirements++;
                        }
                        else
                        {
                            // 找出缺失的设备
                            var missingEquipment = requiredEquipment.Where(req => !availableEquipment.Contains(req)).ToList();

                            conflicts.Add(new SchedulingConflict
                            {
                                ConstraintId = Id,
                                Type = SchedulingConflictType.ClassroomTypeMismatch, // 使用类型不匹配类型，也可以定义新的类型
                                Description = $"Missing equipment for course {assignment.SectionCode} in classroom {assignment.ClassroomName}: " +
                                             $"{string.Join(", ", missingEquipment)}",
                                Severity = ConflictSeverity.Moderate,
                                InvolvedEntities = new Dictionary<string, List<int>>
                                {
                                    { "Sections", new List<int> { assignment.SectionId } },
                                    { "Classrooms", new List<int> { assignment.ClassroomId } }
                                }
                            });
                        }
                    }
                    else
                    {
                        // 没有教室设备信息，添加冲突
                        conflicts.Add(new SchedulingConflict
                        {
                            ConstraintId = Id,
                            Type = SchedulingConflictType.ClassroomTypeMismatch,
                            Description = $"No equipment information available for classroom {assignment.ClassroomName} " +
                                         $"required by course {assignment.SectionCode}",
                            Severity = ConflictSeverity.Minor,
                            InvolvedEntities = new Dictionary<string, List<int>>
                            {
                                { "Sections", new List<int> { assignment.SectionId } },
                                { "Classrooms", new List<int> { assignment.ClassroomId } }
                            }
                        });
                    }
                }
            }

            // 计算满足率作为得分
            double score = totalRequirements > 0 ? (double)satisfiedRequirements / totalRequirements : 1.0;

            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}