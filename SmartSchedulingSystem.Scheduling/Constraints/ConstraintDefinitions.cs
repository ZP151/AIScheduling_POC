using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    /// <summary>
    /// 约束定义类，统一管理所有排课约束
    /// </summary>
    public static class ConstraintDefinitions
    {
        #region 基本排课规则硬约束 (Level1_CoreHard)

        /// <summary>
        /// 教师同时段冲突约束（Level1_CoreHard）
        /// 一个教师在同一时间段只能教授一门课程
        /// </summary>
        public const string TeacherConflict = "TeacherConflict";

        /// <summary>
        /// 教室同时段冲突约束（Level1_CoreHard）
        /// 一个教室在同一时间段只能安排一门课程
        /// </summary>
        public const string ClassroomConflict = "ClassroomConflict";

        /// <summary>
        /// 学生同时段冲突约束（Level1_CoreHard）
        /// 学生不能同时参加两门课程
        /// </summary>
        public const string StudentConflict = "StudentConflict";

        /// <summary>
        /// 教室容量约束（Level1_CoreHard）
        /// 教室容量必须满足课程需求（学生人数）
        /// </summary>
        public const string ClassroomCapacity = "ClassroomCapacity";

        /// <summary>
        /// 课程分配约束（Level1_CoreHard）
        /// 每门课程必须分配到一个时间槽、一个教室和一个教师
        /// </summary>
        public const string CourseAssignment = "CourseAssignment";

        /// <summary>
        /// 课程班级一致性约束（Level1_CoreHard）
        /// 同一课程的不同班级应由同一教师教授（除非特别指定）
        /// </summary>
        public const string CourseSectionConsistency = "CourseSectionConsistency";

        /// <summary>
        /// 先修课程顺序约束（Level1_CoreHard）
        /// 先修课程必须在后续课程之前完成
        /// </summary>
        public const string PrerequisiteOrder = "PrerequisiteOrder";

        /// <summary>
        /// 时间连续性约束（Level1_CoreHard）
        /// 多时段课程必须在连续的时间段进行
        /// </summary>
        public const string TimeContiguity = "TimeContiguity";

        /// <summary>
        /// 互斥时间约束（Level1_CoreHard）
        /// 某些活动必须安排在不同的时间
        /// </summary>
        public const string MutualExclusionTime = "MutualExclusionTime";

        /// <summary>
        /// 最小间隔约束（Level1_CoreHard）
        /// 某些活动之间必须保持最小时间间隔
        /// </summary>
        public const string MinimumTimeGap = "MinimumTimeGap";

        /// <summary>
        /// 关联课程约束（Level1_CoreHard）
        /// 关联的课程（如理论课和实验课）必须按特定规则安排
        /// </summary>
        public const string RelatedCourses = "RelatedCourses";

        #endregion

        #region 可变硬约束 (Level2_ConfigurableHard)

        /// <summary>
        /// 教师可用性约束（Level2_ConfigurableHard）
        /// 教师只能在其可用时间段授课
        /// </summary>
        public const string TeacherAvailability = "TeacherAvailability";

        /// <summary>
        /// 教室可用性约束（Level2_ConfigurableHard）
        /// 教室只能在其可用时间段被使用
        /// </summary>
        public const string ClassroomAvailability = "ClassroomAvailability";

        /// <summary>
        /// 教师最大工作量约束（Level2_ConfigurableHard）
        /// 教师每天/每周的教学时数不能超过最大限制
        /// </summary>
        public const string TeacherMaxWorkload = "TeacherMaxWorkload";

        /// <summary>
        /// 教师资质约束（Level2_ConfigurableHard）
        /// 教师必须有资格教授所分配的课程
        /// </summary>
        public const string TeacherQualification = "TeacherQualification";

        /// <summary>
        /// 课程时段限制约束（Level2_ConfigurableHard）
        /// 某些课程只能在特定时段进行
        /// </summary>
        public const string CourseTimeRestriction = "CourseTimeRestriction";

        #endregion

        #region 物理限制软约束 (Level3_PhysicalSoft)

        /// <summary>
        /// 教室类型匹配约束（Level3_PhysicalSoft）
        /// 课程应分配到合适类型的教室
        /// </summary>
        public const string ClassroomTypeMatch = "ClassroomTypeMatch";

        /// <summary>
        /// 设备需求约束（Level3_PhysicalSoft）
        /// 课程所需设备应与教室设备匹配
        /// </summary>
        public const string EquipmentRequirement = "EquipmentRequirement";

        /// <summary>
        /// 位置临近约束（Level3_PhysicalSoft）
        /// 教师连续课程的教室应尽量临近
        /// </summary>
        public const string LocationProximity = "LocationProximity";

        /// <summary>
        /// 时间可用性软约束（Level3_PhysicalSoft）
        /// 考虑教师和学生的偏好时间
        /// </summary>
        public const string TimeAvailability = "TimeAvailability";

        /// <summary>
        /// 建筑物容量平衡约束（Level3_PhysicalSoft）
        /// 避免同一时间在同一建筑物安排过多课程
        /// </summary>
        public const string BuildingCapacityBalance = "BuildingCapacityBalance";

        /// <summary>
        /// 特殊设施使用率约束（Level3_PhysicalSoft）
        /// 尽量提高特殊设施（如实验室）的使用率
        /// </summary>
        public const string SpecialFacilityUtilization = "SpecialFacilityUtilization";

        #endregion

        #region 质量软约束 (Level4_QualitySoft)

        /// <summary>
        /// 教师偏好约束（Level4_QualitySoft）
        /// 教师应尽量分配其偏好的课程
        /// </summary>
        public const string TeacherPreference = "TeacherPreference";

        /// <summary>
        /// 教师课表紧凑度约束（Level4_QualitySoft）
        /// 教师的课表应尽量紧凑，减少空闲时间
        /// </summary>
        public const string TeacherScheduleCompactness = "TeacherScheduleCompactness";

        /// <summary>
        /// 教师工作量约束（Level4_QualitySoft）
        /// 教师的工作量应尽量均衡
        /// </summary>
        public const string TeacherWorkload = "TeacherWorkload";

        /// <summary>
        /// 学生课表质量约束（Level4_QualitySoft）
        /// 学生课表应避免过长空闲时间和过多连续课程
        /// </summary>
        public const string StudentScheduleQuality = "StudentScheduleQuality";

        /// <summary>
        /// 课程分布均衡约束（Level4_QualitySoft）
        /// 课程应在周内均匀分布
        /// </summary>
        public const string CourseDistribution = "CourseDistribution";

        /// <summary>
        /// 教学连贯性约束（Level4_QualitySoft）
        /// 相关课程应尽量安排在相邻日期
        /// </summary>
        public const string TeachingContinuity = "TeachingContinuity";

        /// <summary>
        /// 课程优先级约束（Level4_QualitySoft）
        /// 高优先级课程应获得更好的时间和教室资源
        /// </summary>
        public const string CoursePriority = "CoursePriority";

        #endregion

        /// <summary>
        /// 获取所有基本排课规则硬约束
        /// </summary>
        public static List<string> GetCoreHardConstraints()
        {
            return new List<string>
            {
                TeacherConflict,
                ClassroomConflict,
                StudentConflict,
                ClassroomCapacity,
                CourseAssignment,
                CourseSectionConsistency,
                TimeContiguity,
                MutualExclusionTime,
                MinimumTimeGap,
                RelatedCourses
            };
        }

        /// <summary>
        /// 获取所有可变硬约束
        /// </summary>
        public static List<string> GetConfigurableHardConstraints()
        {
            return new List<string>
            {
                TeacherAvailability,
                ClassroomAvailability,
                TeacherMaxWorkload,
                TeacherQualification,
                CourseTimeRestriction
            };
        }

        /// <summary>
        /// 获取所有物理限制软约束
        /// </summary>
        public static List<string> GetPhysicalSoftConstraints()
        {
            return new List<string>
            {
                ClassroomTypeMatch,
                EquipmentRequirement,
                LocationProximity,
                TimeAvailability,
                BuildingCapacityBalance,
                SpecialFacilityUtilization
            };
        }

        /// <summary>
        /// 获取所有质量软约束
        /// </summary>
        public static List<string> GetQualitySoftConstraints()
        {
            return new List<string>
            {
                TeacherPreference,
                TeacherScheduleCompactness,
                TeacherWorkload,
                StudentScheduleQuality,
                CourseDistribution,
                TeachingContinuity,
                CoursePriority
            };
        }

        /// <summary>
        /// 获取最小必要的约束（用于简化模式或随机解生成）
        /// </summary>
        public static List<string> GetMinimalEssentialConstraints()
        {
            return new List<string>
            {
                TeacherConflict,
                ClassroomConflict,
                ClassroomCapacity,
                CourseAssignment
            };
        }

        /// <summary>
        /// 获取适用于大学排课的约束
        /// </summary>
        public static List<string> GetUniversityConstraints()
        {
            return new List<string>
            {
                // 核心硬约束
                TeacherConflict,
                ClassroomConflict,
                StudentConflict,
                ClassroomCapacity,
                CourseAssignment,
                PrerequisiteOrder,
                
                // 可变硬约束
                TeacherAvailability,
                ClassroomAvailability,
                TeacherQualification,
                
                // 重要的软约束
                ClassroomTypeMatch,
                EquipmentRequirement,
                TeacherPreference,
                TeacherScheduleCompactness
            };
        }

        /// <summary>
        /// 获取适用于中小学排课的约束
        /// </summary>
        public static List<string> GetK12Constraints()
        {
            return new List<string>
            {
                // 核心硬约束
                TeacherConflict,
                ClassroomConflict,
                ClassroomCapacity,
                CourseAssignment,
                TimeContiguity,
                MinimumTimeGap,
                
                // 可变硬约束
                TeacherAvailability,
                ClassroomAvailability,
                CourseTimeRestriction,
                
                // 重要的软约束
                CourseDistribution,
                TeacherWorkload,
                StudentScheduleQuality
            };
        }

        /// <summary>
        /// 获取适用于考试排课的约束
        /// </summary>
        public static List<string> GetExamConstraints()
        {
            return new List<string>
            {
                // 核心硬约束
                TeacherConflict, // 监考教师
                ClassroomConflict,
                StudentConflict,
                ClassroomCapacity,
                MutualExclusionTime,
                
                // 可变硬约束
                TeacherAvailability,
                ClassroomAvailability,
                
                // 重要的软约束
                MinimumTimeGap, // 考试间隔
                BuildingCapacityBalance
            };
        }

        /// <summary>
        /// 获取约束的层级
        /// </summary>
        public static ConstraintHierarchy GetConstraintHierarchy(string constraintId)
        {
            if (GetCoreHardConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level1_CoreHard;
            }
            else if (GetConfigurableHardConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level2_ConfigurableHard;
            }
            else if (GetPhysicalSoftConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level3_PhysicalSoft;
            }
            else if (GetQualitySoftConstraints().Contains(constraintId))
            {
                return ConstraintHierarchy.Level4_QualitySoft;
            }
            else
            {
                throw new ArgumentException($"未知的约束ID: {constraintId}");
            }
        }

        /// <summary>
        /// 检查约束是否为硬约束
        /// </summary>
        public static bool IsHardConstraint(string constraintId)
        {
            var hierarchy = GetConstraintHierarchy(constraintId);
            return hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                   hierarchy == ConstraintHierarchy.Level2_ConfigurableHard;
        }

        /// <summary>
        /// 从BasicSchedulingRules映射到具体约束
        /// </summary>
        public static List<string> MapBasicRuleToConstraints(string basicRule)
        {
            switch (basicRule)
            {
                case BasicSchedulingRules.ResourceConflictAvoidance:
                    return new List<string> { TeacherConflict, ClassroomConflict, StudentConflict };
                
                case BasicSchedulingRules.ResourceCapacityRespect:
                    return new List<string> { ClassroomCapacity };
                
                case BasicSchedulingRules.AvailabilityRespect:
                    return new List<string> { CourseAssignment };
                
                case BasicSchedulingRules.ResourceAvailability:
                    return new List<string> { TimeContiguity };
                
                case BasicSchedulingRules.ResourcePreference:
                    return new List<string> { CourseSectionConsistency, RelatedCourses };
                
                default:
                    throw new ArgumentException($"未知的基本规则: {basicRule}");
            }
        }
    }

    /// <summary>
    /// 基本排课规则，定义通用的、高层次的排课规则
    /// </summary>
    public static class BasicSchedulingRules
    {
        /// <summary>
        /// 资源冲突避免规则 - 同一资源不能在同一时段被多次使用
        /// </summary>
        public const string ResourceConflictAvoidance = "ResourceConflictAvoidance";

        /// <summary>
        /// 资源容量限制规则 - 资源容量必须满足需求
        /// </summary>
        public const string ResourceCapacityRespect = "ResourceCapacityRespect";

        /// <summary>
        /// 可用性尊重规则 - 资源只能在其可用时间段使用
        /// </summary>
        public const string AvailabilityRespect = "AvailabilityRespect";

        /// <summary>
        /// 资源偏好规则 - 尊重资源的偏好设置
        /// </summary>
        public const string ResourcePreference = "ResourcePreference";

        /// <summary>
        /// 资源可用性规则 - 资源只在可用时间使用
        /// </summary>
        public const string ResourceAvailability = "ResourceAvailability";

        /// <summary>
        /// 获取最基本的、必须遵守的核心规则列表
        /// </summary>
        public static List<string> GetEssentialRules()
        {
            return new List<string>
            {
                ResourceConflictAvoidance,
                ResourceCapacityRespect
            };
        }

        /// <summary>
        /// 获取所有基本规则列表
        /// </summary>
        public static List<string> GetAllRules()
        {
            return new List<string>
            {
                ResourceConflictAvoidance,
                ResourceCapacityRespect,
                AvailabilityRespect,
                ResourcePreference,
                ResourceAvailability
            };
        }
    }
} 