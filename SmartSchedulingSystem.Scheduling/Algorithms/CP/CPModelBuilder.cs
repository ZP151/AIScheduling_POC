using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 构建CP模型的工具类，负责创建约束规划求解器使用的模型
    /// </summary>
    public class CPModelBuilder
    {
        private readonly IEnumerable<ICPConstraintConverter> _constraintConverters;
        private readonly ConstraintManager _constraintManager;

        public CPModelBuilder(IEnumerable<ICPConstraintConverter> constraintConverters, ConstraintManager constraintManager)
        {
            _constraintConverters = constraintConverters ?? throw new ArgumentNullException(nameof(constraintConverters));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
        }

        /// <summary>
        /// 为排课问题构建CP模型
        /// </summary>
        public CpModel BuildModel(SchedulingProblem problem)
        {
            if (problem == null)
                throw new ArgumentNullException(nameof(problem));

            var model = new CpModel();
            var variables = CreateDecisionVariables(model, problem);

            // 添加核心约束
            AddOneCourseOneAssignmentConstraints(model, variables, problem);  // 每门课必须分配一次
            AddTeacherConflictConstraints(model, variables, problem);   // 教师不能同时教两门课
            AddClassroomConflictConstraints(model, variables, problem); // 教室不能同时安排两门课
            AddTeacherAvailabilityConstraints(model, variables, problem); // 教师可用性约束
            AddClassroomAvailabilityConstraints(model, variables, problem); // 教室可用性约束
            AddClassroomCapacityConstraints(model, variables, problem); // 教室容量约束
            AddPrerequisiteConstraints(model, variables, problem);      // 先修课程约束


            // 应用所有自定义约束转换器
            foreach (var converter in _constraintConverters)
            {
                converter.AddToModel(model, variables, problem);
            }

            // 设置目标函数（最大化软约束满足度）
            SetupObjectiveFunction(model, variables, problem);

            return model;
        }

        /// <summary>
        /// 创建决策变量
        /// </summary>
        private Dictionary<string, IntVar> CreateDecisionVariables(CpModel model, SchedulingProblem problem)
        {
            var variables = new Dictionary<string, IntVar>();
            int variableCount = 0;
            // 为每个课程-时间-教室-教师的可能组合创建二元变量
            foreach (var course in problem.CourseSections)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    foreach (var classroom in problem.Classrooms)
                    {
                        // 检查教室容量是否满足课程需求的基本过滤
                        if (classroom.Capacity < course.Enrollment)
                            continue;

                        foreach (var teacher in problem.Teachers)
                        {
                            Console.WriteLine($"考虑变量: 课程={course.Id}, 时间={timeSlot.Id}, 教室={classroom.Id}, 教师={teacher.Id}");

                            //// 基本筛选：检查教师是否可以教授此课程
                            //bool teacherCanTeachCourse =
                            //    problem.TeacherCoursePreferences
                            //        .Any(tcp => tcp.TeacherId == teacher.Id &&
                            //                   tcp.CourseId == course.CourseId &&
                            //                   tcp.ProficiencyLevel >= 2);

                            //if (!teacherCanTeachCourse)
                            //    continue;

                            //// 检查教师在此时间段是否可用
                            //bool teacherAvailable =
                            //    !problem.TeacherAvailabilities
                            //        .Any(ta => ta.TeacherId == teacher.Id &&
                            //                  ta.TimeSlotId == timeSlot.Id &&
                            //                  !ta.IsAvailable);

                            //if (!teacherAvailable)
                            //    continue;

                            //// 检查教室在此时间段是否可用
                            //bool classroomAvailable =
                            //    !problem.ClassroomAvailabilities
                            //        .Any(ca => ca.ClassroomId == classroom.Id &&
                            //                  ca.TimeSlotId == timeSlot.Id &&
                            //                  !ca.IsAvailable);

                            //if (!classroomAvailable)
                            //    continue;

                            // 创建唯一标识符
                            string varName = $"c{course.Id}_t{timeSlot.Id}_r{classroom.Id}_f{teacher.Id}";

                            // 创建0-1整数变量(0=不分配，1=分配)
                            var variable = model.NewBoolVar(varName);
                            variables[varName] = variable;
                            variableCount++;

                        }
                    }
                }
            }
            Console.WriteLine($"总共创建了 {variableCount} 个决策变量");

            // 如果某门课程没有可行的分配，记录日志
            foreach (var course in problem.CourseSections)
            {
                var courseVars = variables.Where(kv => kv.Key.StartsWith($"c{course.Id}_")).ToList();
                if (courseVars.Count == 0)
                {
                    Console.WriteLine($"警告: 课程 {course.Id} ({course.CourseName}) 没有可行的分配，可能无法生成有效解");
                }
            }

            return variables;
        }

        /// <summary>
        /// 设置目标函数，优化软约束满足度
        /// </summary>
        private void SetupObjectiveFunction(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            // 创建目标函数表达式
            LinearExpr objective = LinearExpr.Constant(0);

            // 1. 教师偏好满足度（教师在自己偏好的时间段教课）
            foreach (var teacher in problem.Teachers)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 获取教师对此时间段的偏好级别（1-5）
                    int preferenceLevel = 3; // 默认中等偏好

                    var preference = problem.TeacherAvailabilities
                        .FirstOrDefault(ta => ta.TeacherId == teacher.Id && ta.TimeSlotId == timeSlot.Id);

                    if (preference != null && preference.PreferenceLevel > 0)
                    {
                        preferenceLevel = preference.PreferenceLevel;
                    }

                    // 找到所有教师在此时间段的变量
                    var teacherTimeVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_") && kv.Key.EndsWith($"_f{teacher.Id}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    if (teacherTimeVars.Count > 0)
                    {
                        // 将教师偏好级别添加到目标函数
                        foreach (var variable in teacherTimeVars)
                        {
                            objective += preferenceLevel * variable;
                        }
                    }
                }
            }

            // 2. 教室类型匹配度
            foreach (var course in problem.CourseSections)
            {
                foreach (var classroom in problem.Classrooms)
                {
                    // 计算课程与教室类型的匹配度（0-5）
                    int matchScore = CalculateRoomTypeMatchScore(course, classroom, problem);

                    // 找到所有此课程使用此教室的变量
                    var courseRoomVars = variables
                        .Where(kv => kv.Key.StartsWith($"c{course.Id}_") && kv.Key.Contains($"_r{classroom.Id}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    if (courseRoomVars.Count > 0)
                    {
                        // 将匹配度添加到目标函数
                        foreach (var variable in courseRoomVars)
                        {
                            objective += matchScore * variable;
                        }
                    }
                }
            }

            // 3. 教室容量适合度（避免小班大教室或大班小教室）
            foreach (var course in problem.CourseSections)
            {
                foreach (var classroom in problem.Classrooms)
                {
                    // 计算容量适合度（0-5）
                    int capacityScore = CalculateCapacityScore(course.Enrollment, classroom.Capacity);

                    // 找到所有此课程使用此教室的变量
                    var courseRoomVars = variables
                        .Where(kv => kv.Key.StartsWith($"c{course.Id}_") && kv.Key.Contains($"_r{classroom.Id}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    if (courseRoomVars.Count > 0)
                    {
                        // 将容量适合度添加到目标函数
                        foreach (var variable in courseRoomVars)
                        {
                            objective += capacityScore * variable;
                        }
                    }
                }
            }

            // 设置目标：最大化总分数
            model.Maximize(objective);
        }

        /// <summary>
        /// 计算课程与教室类型的匹配度
        /// </summary>
        private int CalculateRoomTypeMatchScore(CourseSectionInfo course, ClassroomInfo classroom, SchedulingProblem problem)
        {
            // 这里进行简化评分
            // 5分：完美匹配
            // 3分：基本满足
            // 1分：勉强可用
            // 0分：不匹配

            // 如果课程要求特定教室类型
            if (!string.IsNullOrEmpty(course.RequiredRoomType))
            {
                if (course.RequiredRoomType.Equals(classroom.Type, StringComparison.OrdinalIgnoreCase))
                {
                    return 5; // 完美匹配
                }

                // 检查是否为可接受的替代类型
                if (IsCompatibleRoomType(course.RequiredRoomType, classroom.Type))
                {
                    return 3; // 基本满足
                }

                return 0; // 不匹配
            }

            // 如果课程有设备需求
            if (!string.IsNullOrEmpty(course.RequiredEquipment))
            {
                var requiredEquipments = course.RequiredEquipment.Split(',');
                var availableEquipments = classroom.Equipment?.Split(',') ?? new string[0];

                // 计算满足的设备比例
                int matchedEquipments = requiredEquipments
                    .Count(req => availableEquipments.Any(avail =>
                        avail.Trim().Equals(req.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (matchedEquipments == requiredEquipments.Length)
                {
                    return 5; // 完全满足设备需求
                }

                if (matchedEquipments > 0)
                {
                    return 3; // 部分满足设备需求
                }

                return 1; // 没有满足设备需求
            }

            // 默认为普通教室，任何教室都可接受
            return 3;
        }

        /// <summary>
        /// 判断两种教室类型是否兼容
        /// </summary>
        private bool IsCompatibleRoomType(string requiredType, string actualType)
        {
            // 检查常见的兼容类型
            if (requiredType.Contains("lecture", StringComparison.OrdinalIgnoreCase))
            {
                // 讲课教室可以在大型教室、多媒体教室等进行
                return actualType.Contains("large", StringComparison.OrdinalIgnoreCase) ||
                       actualType.Contains("multimedia", StringComparison.OrdinalIgnoreCase);
            }

            if (requiredType.Contains("lab", StringComparison.OrdinalIgnoreCase))
            {
                // 实验课必须在实验室，不能替代
                return actualType.Contains("lab", StringComparison.OrdinalIgnoreCase);
            }

            if (requiredType.Contains("computer", StringComparison.OrdinalIgnoreCase))
            {
                // 计算机课必须在计算机房
                return actualType.Contains("computer", StringComparison.OrdinalIgnoreCase);
            }

            // 其他情况视为不兼容
            return false;
        }
        private void AddOneCourseOneAssignmentConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var course in problem.CourseSections)
            {
                // 找出所有涉及此课程班级的变量
                var courseVars = variables
                    .Where(kv => kv.Key.StartsWith($"c{course.Id}_"))
                    .Select(kv => kv.Value)
                    .ToList();

                if (courseVars.Count > 0)
                {
                    // 约束：每门课程必须且只能分配一次
                    model.Add(LinearExpr.Sum(courseVars) == 1);
                }
            }
        }

        private void AddTeacherConflictConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var teacher in problem.Teachers)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 找出该教师在该时间段的所有可能分配
                    var teacherTimeVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_") &&
                                   kv.Key.EndsWith($"_f{teacher.Id}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    if (teacherTimeVars.Count > 1)
                    {
                        // 约束：教师在同一时间段最多只能教一门课
                        model.Add(LinearExpr.Sum(teacherTimeVars) <= 1);
                    }
                }
            }
        }

        private void AddClassroomConflictConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var classroom in problem.Classrooms)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 找出该教室在该时间段的所有可能分配
                    var roomTimeVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_r{classroom.Id}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    if (roomTimeVars.Count > 1)
                    {
                        // 约束：教室在同一时间段最多只能安排一门课
                        model.Add(LinearExpr.Sum(roomTimeVars) <= 1);
                    }
                }
            }
        }

        private void AddTeacherAvailabilityConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var availability in problem.TeacherAvailabilities)
            {
                if (!availability.IsAvailable)
                {
                    // 找出教师在不可用时间段的所有可能分配
                    var unavailableVars = variables
                        .Where(kv => kv.Key.Contains($"_t{availability.TimeSlotId}_") &&
                                   kv.Key.EndsWith($"_f{availability.TeacherId}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    foreach (var variable in unavailableVars)
                    {
                        // 约束：不可用时间段的分配变量必须为0
                        model.Add(variable == 0);
                    }
                }
            }
        }

        private void AddClassroomAvailabilityConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var availability in problem.ClassroomAvailabilities)
            {
                if (!availability.IsAvailable)
                {
                    // 找出教室在不可用时间段的所有可能分配
                    var unavailableVars = variables
                        .Where(kv => kv.Key.Contains($"_t{availability.TimeSlotId}_r{availability.ClassroomId}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    foreach (var variable in unavailableVars)
                    {
                        // 约束：不可用时间段的分配变量必须为0
                        model.Add(variable == 0);
                    }
                }
            }
        }

        private void AddClassroomCapacityConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var section in problem.CourseSections)
            {
                foreach (var classroom in problem.Classrooms)
                {
                    if (classroom.Capacity < section.Enrollment)
                    {
                        // 找出教室容量不足的所有可能分配
                        var invalidVars = variables
                            .Where(kv => kv.Key.StartsWith($"c{section.Id}_") &&
                                       kv.Key.Contains($"_r{classroom.Id}_"))
                            .Select(kv => kv.Value)
                            .ToList();

                        foreach (var variable in invalidVars)
                        {
                            // 约束：容量不足的教室不能安排此课程
                            model.Add(variable == 0);
                        }
                    }
                }
            }
        }

        private void AddPrerequisiteConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            // 创建课程ID到班级ID的映射
            var courseToSections = new Dictionary<int, List<int>>();
            foreach (var section in problem.CourseSections)
            {
                if (!courseToSections.ContainsKey(section.CourseId))
                {
                    courseToSections[section.CourseId] = new List<int>();
                }
                courseToSections[section.CourseId].Add(section.Id);
            }

            // 处理先修课程约束
            // 在当前学期内，先修课程不能与后续课程在同一时间段
            foreach (var section in problem.CourseSections)
            {
                var course = section.Course;

                if (course?.Prerequisites == null || course.Prerequisites.Count == 0)
                    continue;

                foreach (var prerequisite in course.Prerequisites)
                {
                    // 当前课程和先修课程分别对应的 CourseSection 列表
                    if (courseToSections.TryGetValue(course.CourseId, out var sectionIds) &&
                        courseToSections.TryGetValue(prerequisite.PrerequisiteCourseId, out var prereqSectionIds))
                    {
                        foreach (var timeSlot in problem.TimeSlots)
                        {
                            foreach (var sectionId in sectionIds)
                            {
                                foreach (var prereqSectionId in prereqSectionIds)
                                {
                                    var sectionTimeVars = variables
                                        .Where(kv => kv.Key.StartsWith($"c{sectionId}_t{timeSlot.Id}_"))
                                        .Select(kv => kv.Value)
                                        .ToList();

                                    var prereqTimeVars = variables
                                        .Where(kv => kv.Key.StartsWith($"c{prereqSectionId}_t{timeSlot.Id}_"))
                                        .Select(kv => kv.Value)
                                        .ToList();

                                    foreach (var sectionVar in sectionTimeVars)
                                    {
                                        foreach (var prereqVar in prereqTimeVars)
                                        {
                                            // 添加：先修课程与课程不能同时安排
                                            model.Add(sectionVar + prereqVar <= 1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 计算教室容量适合度
        /// </summary>
        private int CalculateCapacityScore(int enrollment, int capacity)
        {
            if (capacity < enrollment)
            {
                // 容量不足，不可用
                return 0;
            }

            // 计算容量利用率
            double utilizationRatio = (double)enrollment / capacity;

            if (utilizationRatio > 0.85)
            {
                // 利用率很高，接近满员但不超过（最理想）
                return 5;
            }

            if (utilizationRatio > 0.7)
            {
                // 利用率较高
                return 4;
            }

            if (utilizationRatio > 0.5)
            {
                // 适中利用率
                return 3;
            }

            if (utilizationRatio > 0.3)
            {
                // 利用率较低
                return 2;
            }

            // 利用率很低，浪费空间
            return 1;
        }
    }
}