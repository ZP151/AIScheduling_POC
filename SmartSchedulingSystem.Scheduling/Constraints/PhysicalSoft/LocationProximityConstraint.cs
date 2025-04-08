using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints.PhysicalSoft
{
    public class LocationProximityConstraint : IConstraint
    {
        private readonly Dictionary<int, int> _teacherDepartmentIds;
        private readonly Dictionary<int, int> _buildingCampusIds;
        private readonly Dictionary<(int, int), int> _campusTravelTimes;

        public int Id { get; } = 10;
        public string Name { get; } = "Location Proximity";
        public string Description { get; } = "Ensures appropriate travel time between buildings and campuses";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.7;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level2_PhysicalSoft;
        public string Category => "Physical Resources";

        public LocationProximityConstraint(
            Dictionary<int, int> teacherDepartmentIds,
            Dictionary<int, int> buildingCampusIds,
            Dictionary<(int, int), int> campusTravelTimes)
        {
            _teacherDepartmentIds = teacherDepartmentIds;
            _buildingCampusIds = buildingCampusIds;
            _campusTravelTimes = campusTravelTimes;
        }

        public LocationProximityConstraint()
        {
            // 默认构造函数
            _teacherDepartmentIds = new Dictionary<int, int>();
            _buildingCampusIds = new Dictionary<int, int>();
            _campusTravelTimes = new Dictionary<(int, int), int>();
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            var conflicts = new List<SchedulingConflict>();

            // 按教师分组
            var teacherGroups = solution.Assignments
                .GroupBy(a => a.TeacherId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 对每位教师，检查他们当天的课程安排
            foreach (var teacherGroup in teacherGroups)
            {
                var teacherId = teacherGroup.Key;
                var assignments = teacherGroup.Value
                    .OrderBy(a => a.DayOfWeek)
                    .ThenBy(a => a.StartTime)
                    .ToList();

                // 按天分组
                var dailyAssignments = assignments
                    .GroupBy(a => a.DayOfWeek)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 检查每天的安排
                foreach (var dayGroup in dailyAssignments)
                {
                    var dayAssignments = dayGroup.Value;

                    // 检查连续课程之间的位置
                    for (int i = 0; i < dayAssignments.Count - 1; i++)
                    {
                        var current = dayAssignments[i];
                        var next = dayAssignments[i + 1];

                        // 获取教室所在建筑/校区
                        int currentBuildingId = GetBuildingId(current.ClassroomId);
                        int nextBuildingId = GetBuildingId(next.ClassroomId);

                        int currentCampusId = _buildingCampusIds.GetValueOrDefault(currentBuildingId);
                        int nextCampusId = _buildingCampusIds.GetValueOrDefault(nextBuildingId);

                        // 计算两节课之间的时间间隔（分钟）
                        var timeBetween = (next.StartTime - current.EndTime).TotalMinutes;

                        // 如果在不同建筑
                        if (currentBuildingId != nextBuildingId)
                        {
                            // 如果在不同校区
                            if (currentCampusId != nextCampusId)
                            {
                                // 检查是否有足够的校区间旅行时间
                                int requiredTravelTime = _campusTravelTimes.GetValueOrDefault((currentCampusId, nextCampusId), 30);

                                if (timeBetween < requiredTravelTime)
                                {
                                    conflicts.Add(new SchedulingConflict
                                    {
                                        ConstraintId = Id,
                                        Type = SchedulingConflictType.CampusTravelTimeConflict,
                                        Description = $"Teacher {current.TeacherName} has insufficient time ({timeBetween} min) for travel between campuses (requires {requiredTravelTime} min)",
                                        Severity = ConflictSeverity.Moderate,
                                        InvolvedEntities = new Dictionary<string, List<int>>
                                    {
                                        { "Teachers", new List<int> { teacherId } },
                                        { "Sections", new List<int> { current.SectionId, next.SectionId } }
                                    },
                                        InvolvedTimeSlots = new List<int> { current.TimeSlotId, next.TimeSlotId }
                                    });
                                }
                            }
                            else
                            {
                                // 同校区不同建筑，检查是否有足够的建筑间移动时间
                                if (timeBetween < 15) // 假设建筑间需要15分钟
                                {
                                    conflicts.Add(new SchedulingConflict
                                    {
                                        ConstraintId = Id,
                                        Type = SchedulingConflictType.BuildingProximityConflict,
                                        Description = $"Teacher {current.TeacherName} has insufficient time ({timeBetween} min) for travel between buildings",
                                        Severity = ConflictSeverity.Minor,
                                        InvolvedEntities = new Dictionary<string, List<int>>
                                    {
                                        { "Teachers", new List<int> { teacherId } },
                                        { "Sections", new List<int> { current.SectionId, next.SectionId } }
                                    },
                                        InvolvedTimeSlots = new List<int> { current.TimeSlotId, next.TimeSlotId }
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // 计算得分
            double score = conflicts.Count == 0 ? 1.0 : Math.Max(0, 1.0 - (conflicts.Count * 0.1));

            return (score, conflicts);
        }

        private int GetBuildingId(int classroomId)
        {
            // 这里需要实现获取教室所在建筑的逻辑
            // 简化实现，实际应该从数据库或其他数据源获取
            return classroomId % 100; // 假设根据教室ID计算建筑ID
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}
