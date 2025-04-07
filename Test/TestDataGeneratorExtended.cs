using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Constraints.Hard;
using SmartSchedulingSystem.Scheduling.Constraints.PhysicalSoft;
using SmartSchedulingSystem.Scheduling.Constraints.QualitySoft;
using SmartSchedulingSystem.Scheduling.Constraints.Soft;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System.Collections.Generic;
using System.Data;

namespace SmartSchedulingSystem.Test
{
    /// <summary>
    /// 测试数据生成器扩展，添加约束初始化功能
    /// </summary>
    public class TestDataGeneratorExtended
    {
        private readonly TestDataGenerator _baseGenerator;

        public TestDataGeneratorExtended(TestDataGenerator baseGenerator)
        {
            _baseGenerator = baseGenerator;
        }

        /// <summary>
        /// 生成测试排课问题，并添加必要的约束条件
        /// </summary>
        public SchedulingProblem GenerateTestProblemWithConstraints(
            int courseSectionCount = 20,
            int teacherCount = 10,
            int classroomCount = 15,
            int timeSlotCount = 30)
        {
            // 生成基础问题数据
            var problem = _baseGenerator.GenerateTestProblem(
                courseSectionCount,
                teacherCount,
                classroomCount,
                timeSlotCount);

            // 添加约束条件
            problem.Constraints = InitializeConstraints(problem);

            return problem;
        }

        /// <summary>
        /// 初始化约束列表
        /// </summary>
        private List<IConstraint> InitializeConstraints(SchedulingProblem problem)
        {
            var constraints = new List<IConstraint>();

            // 1. 添加硬约束
            AddHardConstraints(constraints, problem);

            // 2. 添加物理软约束
            AddPhysicalSoftConstraints(constraints, problem);

            // 3. 添加质量软约束
            AddQualitySoftConstraints(constraints, problem);

            return constraints;
        }

        /// <summary>
        /// 添加硬约束
        /// </summary>
        private void AddHardConstraints(List<IConstraint> constraints, SchedulingProblem problem)
        {
            // 教师冲突约束
            constraints.Add(new TeacherConflictConstraint());

            // 教室冲突约束
            constraints.Add(new ClassroomConflictConstraint());

            // 教师可用性约束
            var teacherAvailability = ConvertTeacherAvailabilities(problem.TeacherAvailabilities);
            constraints.Add(new TeacherAvailabilityConstraint(teacherAvailability));

            // 教室可用性约束
            var classroomAvailability = ConvertClassroomAvailabilities(problem.ClassroomAvailabilities);
            constraints.Add(new ClassroomAvailabilityConstraint(classroomAvailability));

            // 教室容量约束
            var classroomCapacities = GetClassroomCapacities(problem.Classrooms);
            var expectedEnrollments = GetExpectedEnrollments(problem.CourseSections);
            constraints.Add(new ClassroomCapacityConstraint(classroomCapacities, expectedEnrollments));

            // 先修课程约束（简化版，假设没有先修课程）
            var courseSectionMap = GetCourseSectionMap(problem.CourseSections);
            constraints.Add(new PrerequisiteConstraint(new Dictionary<int, List<int>>(), courseSectionMap));
        }

        /// <summary>
        /// 添加物理软约束
        /// </summary>
        private void AddPhysicalSoftConstraints(List<IConstraint> constraints, SchedulingProblem problem)
        {
            // 教室类型匹配约束
            var courseSectionTypes = GetCourseSectionTypes(problem.CourseSections);
            var classroomTypes = GetClassroomTypes(problem.Classrooms);

            // 设备需求约束
            var sectionRequiredEquipment = GetSectionRequiredEquipment(problem.CourseSections);
            var classroomEquipment = GetClassroomEquipment(problem.Classrooms);
            constraints.Add(new EquipmentRequirementConstraint(sectionRequiredEquipment, classroomEquipment));

            // 时间可用性约束
            var unavailablePeriods = new List<(DateTime Start, DateTime End, string Reason)>();
            var semesterDates = new Dictionary<int, (DateTime Start, DateTime End)>
            {
                { problem.Id, (DateTime.Now.Date, DateTime.Now.Date.AddMonths(4)) }
            };
            constraints.Add(new TimeAvailabilityConstraint(unavailablePeriods, semesterDates));

            // 位置邻近性约束
            var teacherDepartmentIds = GetTeacherDepartmentIds(problem.Teachers);
            var buildingCampusIds = GetBuildingCampusIds(problem.Classrooms);
            var campusTravelTimes = GetCampusTravelTimes();
            constraints.Add(new LocationProximityConstraint(teacherDepartmentIds, buildingCampusIds, campusTravelTimes));
        }

        /// <summary>
        /// 添加质量软约束
        /// </summary>
        private void AddQualitySoftConstraints(List<IConstraint> constraints, SchedulingProblem problem)
        {
            // 教师偏好约束
            var teacherPreferences = ConvertTeacherPreferences(problem.TeacherAvailabilities);
            constraints.Add(new TeacherPreferenceConstraint(teacherPreferences));

            // 教师工作量约束
            var maxWeeklyHours = GetTeacherMaxWeeklyHours(problem.Teachers);
            var maxDailyHours = GetTeacherMaxDailyHours(problem.Teachers);
            constraints.Add(new TeacherWorkloadConstraint(maxWeeklyHours, maxDailyHours));

            // 教师排课紧凑性约束
            constraints.Add(new TeacherScheduleCompactnessConstraint());
        }

        #region Helper Methods for Constraint Initialization

        private Dictionary<(int TeacherId, int TimeSlotId), bool> ConvertTeacherAvailabilities(
            List<TeacherAvailability> availabilities)
        {
            var result = new Dictionary<(int TeacherId, int TimeSlotId), bool>();

            foreach (var availability in availabilities)
            {
                result[(availability.TeacherId, availability.TimeSlotId)] = availability.IsAvailable;
            }

            return result;
        }

        private Dictionary<(int ClassroomId, int TimeSlotId), bool> ConvertClassroomAvailabilities(
            List<ClassroomAvailability> availabilities)
        {
            var result = new Dictionary<(int ClassroomId, int TimeSlotId), bool>();

            foreach (var availability in availabilities)
            {
                result[(availability.ClassroomId, availability.TimeSlotId)] = availability.IsAvailable;
            }

            return result;
        }

        private Dictionary<int, int> GetClassroomCapacities(List<ClassroomInfo> classrooms)
        {
            return classrooms.ToDictionary(c => c.Id, c => c.Capacity);
        }

        private Dictionary<int, int> GetExpectedEnrollments(List<CourseSectionInfo> sections)
        {
            return sections.ToDictionary(s => s.Id, s => s.Enrollment);
        }

        private Dictionary<int, int> GetCourseSectionMap(List<CourseSectionInfo> sections)
        {
            return sections.ToDictionary(s => s.Id, s => s.CourseId);
        }

        private Dictionary<int, string> GetCourseSectionTypes(List<CourseSectionInfo> sections)
        {
            return sections.ToDictionary(s => s.Id, s => s.CourseType);
        }

        private Dictionary<int, string> GetClassroomTypes(List<ClassroomInfo> classrooms)
        {
            return classrooms.ToDictionary(c => c.Id, c => c.Type);
        }

        private Dictionary<int, List<string>> GetSectionRequiredEquipment(List<CourseSectionInfo> sections)
        {
            var result = new Dictionary<int, List<string>>();

            foreach (var section in sections)
            {
                if (!string.IsNullOrEmpty(section.RequiredEquipment))
                {
                    result[section.Id] = section.RequiredEquipment.Split(',').Select(e => e.Trim()).ToList();
                }
                else
                {
                    result[section.Id] = new List<string>();
                }
            }

            return result;
        }

        private Dictionary<int, List<string>> GetClassroomEquipment(List<ClassroomInfo> classrooms)
        {
            var result = new Dictionary<int, List<string>>();

            foreach (var classroom in classrooms)
            {
                if (!string.IsNullOrEmpty(classroom.Equipment))
                {
                    result[classroom.Id] = classroom.Equipment.Split(',').Select(e => e.Trim()).ToList();
                }
                else
                {
                    result[classroom.Id] = new List<string>();
                }
            }

            return result;
        }

        private Dictionary<int, int> GetTeacherDepartmentIds(List<TeacherInfo> teachers)
        {
            return teachers.ToDictionary(t => t.Id, t => t.DepartmentId);
        }

        private Dictionary<int, int> GetBuildingCampusIds(List<ClassroomInfo> classrooms)
        {
            // 假设每个教室都有校区ID属性
            return classrooms.ToDictionary(c => c.Id, c => c.CampusId);
        }

        private Dictionary<(int, int), int> GetCampusTravelTimes()
        {
            // 简化版：只有两个校区，相互间需要30分钟
            var result = new Dictionary<(int, int), int>
            {
                { (1, 2), 30 },
                { (2, 1), 30 }
            };

            return result;
        }

        private Dictionary<(int TeacherId, int TimeSlotId), int> ConvertTeacherPreferences(
            List<TeacherAvailability> availabilities)
        {
            var result = new Dictionary<(int TeacherId, int TimeSlotId), int>();

            foreach (var availability in availabilities)
            {
                if (availability.IsAvailable) // 只记录可用时间的偏好
                {
                    result[(availability.TeacherId, availability.TimeSlotId)] = availability.PreferenceLevel;
                }
            }

            return result;
        }

        private Dictionary<int, int> GetTeacherMaxWeeklyHours(List<TeacherInfo> teachers)
        {
            return teachers.ToDictionary(t => t.Id, t => t.MaxWeeklyHours);
        }

        private Dictionary<int, int> GetTeacherMaxDailyHours(List<TeacherInfo> teachers)
        {
            return teachers.ToDictionary(t => t.Id, t => t.MaxDailyHours);
        }

        #endregion
    }
}