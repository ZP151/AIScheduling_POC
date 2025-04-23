using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;
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
        private Dictionary<string, IntVar> _variables = new Dictionary<string, IntVar>();
        private readonly ILogger<CPScheduler> _logger;
        public CPModelBuilder(IEnumerable<ICPConstraintConverter> constraintConverters, ConstraintManager constraintManager, ILogger<CPScheduler> logger)
        {
            _constraintConverters = constraintConverters ?? throw new ArgumentNullException(nameof(constraintConverters));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }
        public Dictionary<string, IntVar> GetVariables()
        {
            return _variables;
        }
        /// <summary>
        /// 为排课问题构建CP模型
        /// </summary>
        public CpModel BuildModel(SchedulingProblem problem, ConstraintApplicationLevel level = ConstraintApplicationLevel.Basic)
        {
            _variables.Clear();

            Console.WriteLine("============ CP模型构建开始 ============");
            Console.WriteLine($"Problem详情: {problem.Name}, {problem.CourseSections.Count}门课程");
            Console.WriteLine($"约束应用级别: {level}");

            if (problem == null)
                throw new ArgumentNullException(nameof(problem));

            var model = new CpModel();
            _variables = CreateDecisionVariables(model, problem);

            // 添加核心约束 (Level1_CoreHard)
            Console.WriteLine("添加OneCourseOneAssignment约束");
            AddOneCourseOneAssignmentConstraints(model, _variables, problem);  // 每门课必须分配一次
            
            Console.WriteLine("添加教师冲突约束");
            AddTeacherConflictConstraints(model, _variables, problem);   // 教师不能同时教两门课
            
            Console.WriteLine("添加教室冲突约束");
            AddClassroomConflictConstraints(model, _variables, problem); // 教室不能同时安排两门课

            // 根据约束级别选择性添加约束
            if (level >= ConstraintApplicationLevel.Basic)
            {
                Console.WriteLine("添加教室容量约束");
                AddClassroomCapacityConstraints(model, _variables, problem); // 教室容量约束
                
                Console.WriteLine("添加先修课程约束");
                AddPrerequisiteConstraints(model, _variables, problem);      // 先修课程约束
            }

            // 只在更高级别添加Level2及以上的约束
            if (level >= ConstraintApplicationLevel.Standard)
            {
                Console.WriteLine("添加教师可用性约束");
                AddTeacherAvailabilityConstraints(model, _variables, problem); // 教师可用性约束 (Level2)
                
                Console.WriteLine("添加教室可用性约束");
                AddClassroomAvailabilityConstraints(model, _variables, problem); // 教室可用性约束 (Level2)
            }

            // 应用当前约束级别允许的自定义约束转换器
            foreach (var converter in _constraintConverters)
            {
                // 只应用当前级别允许的约束转换器
                if (IsConverterAllowedAtLevel(converter, level))
                {
                    Console.WriteLine($"应用约束转换器: {converter.GetType().Name}");
                    converter.AddToModel(model, _variables, problem);
                }
            }

            // 设置目标函数（最大化软约束满足度）
            Console.WriteLine("设置目标函数");
            SetupObjectiveFunction(model, _variables, problem);
            Console.WriteLine("============ CP模型构建完成 ============");

            return model;
        }

        /// <summary>
        /// 判断约束转换器是否允许在当前约束级别应用
        /// </summary>
        private bool IsConverterAllowedAtLevel(ICPConstraintConverter converter, ConstraintApplicationLevel level)
        {
            // 根据转换器类型判断其约束级别
            string converterName = converter.GetType().Name;
            
            // 核心硬约束转换器(Level1) - 在所有级别都允许
            if (converterName.Contains("TeacherConflict") || 
                converterName.Contains("ClassroomConflict") ||
                converterName.Contains("ClassroomCapacity") ||
                converterName.Contains("Prerequisite"))
            {
                return true;
            }
            
            // 可变硬约束转换器(Level2) - 只在Standard及以上级别允许
            if (level >= ConstraintApplicationLevel.Standard &&
                (converterName.Contains("TeacherAvailability") || 
                 converterName.Contains("ClassroomAvailability")))
            {
                return true;
            }
            
            // 软约束转换器(Level3和Level4) - 只在Complete级别允许
            if (level >= ConstraintApplicationLevel.Complete)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 创建决策变量 - 优化版本，减少变量数量以提高求解速度
        /// </summary>
        private Dictionary<string, IntVar> CreateDecisionVariables(CpModel model, SchedulingProblem problem)
        {
            var variables = new Dictionary<string, IntVar>();
            int variableCount = 0;

            _logger.LogInformation($"开始创建决策变量: 课程数={problem.CourseSections.Count}, " +
                                 $"教师数={problem.Teachers.Count}, 教室数={problem.Classrooms.Count}, " +
                                 $"时间槽数={problem.TimeSlots.Count}");

            // 检查基本条件
            if (problem.CourseSections.Count == 0 || problem.Teachers.Count == 0 ||
                problem.Classrooms.Count == 0 || problem.TimeSlots.Count == 0)
            {
                _logger.LogError("创建变量失败：课程、教师、教室或时间槽数量为0");
                return variables;
            }

            // 为每门课程创建变量 - 采用预筛选方式减少变量数量
            foreach (var section in problem.CourseSections)
            {
                bool sectionHasVariables = false;
                _logger.LogDebug($"为课程 {section.Id} ({section.CourseName}) 创建变量...");

                // 筛选满足容量要求的教室，减少变量数量
                var suitableRooms = problem.Classrooms
                    .Where(room => room.Capacity >= section.Enrollment)
                    .ToList();

                if (suitableRooms.Count == 0)
                {
                    _logger.LogWarning($"警告: 课程 {section.Id} ({section.CourseName}) 容量为 {section.Enrollment}，没有找到容量足够的教室");
                    
                    // 如果没有容量足够的教室，选择容量最大的几个教室
                    suitableRooms = problem.Classrooms
                        .OrderByDescending(room => room.Capacity)
                        .Take(3)
                        .ToList();
                    
                    _logger.LogWarning($"为避免无解，选择了 {suitableRooms.Count} 个容量最大的教室");
                }

                // 筛选有资格教授此课程的教师，减少变量数量
                var qualifiedTeachers = new List<TeacherInfo>();
                
                // 查找教师课程偏好
                var teacherPreferences = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 2)
                    .ToList();
                
                if (teacherPreferences.Count > 0)
                {
                    // 基于偏好选择教师
                    var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                    qualifiedTeachers = problem.Teachers
                        .Where(t => preferredTeacherIds.Contains(t.Id))
                        .ToList();
                }
                
                if (qualifiedTeachers.Count == 0)
                {
                    _logger.LogWarning($"警告: 课程 {section.Id} ({section.CourseName}) 没有合格的教师");
                    
                    // 如果没有合格的教师，选择所有教师避免无解
                    qualifiedTeachers = problem.Teachers.ToList();
                    _logger.LogWarning($"为避免无解，选择了所有 {qualifiedTeachers.Count} 个教师");
                }

                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 初始时忽略可用性约束，以便生成更多可能的变量
                    var availableRooms = suitableRooms;
                    var availableTeachers = qualifiedTeachers;

                    foreach (var classroom in availableRooms)
                    {
                        foreach (var teacher in availableTeachers)
                        {
                            // 创建变量
                            string varName = $"c{section.Id}_t{timeSlot.Id}_r{classroom.Id}_f{teacher.Id}";
                            var variable = model.NewBoolVar(varName);
                            variables[varName] = variable;
                            variableCount++;
                            sectionHasVariables = true;

                            if (variableCount % 1000 == 0)
                            {
                                _logger.LogInformation($"已创建 {variableCount} 个变量...");
                            }
                        }
                    }
                }

                if (!sectionHasVariables)
                {
                    _logger.LogWarning($"课程 {section.Id} ({section.CourseName}) 没有创建任何变量，可能无法生成有效解");
                }
            }

            _logger.LogInformation($"变量创建完成，总共创建了 {variables.Count} 个变量");
            return variables;
        }

        /// <summary>
        /// 设置目标函数，优化软约束满足度
        /// </summary>
        private void SetupObjectiveFunction(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            // 创建目标函数项列表
            var terms = new List<IntVar>();
            var coefficients = new List<int>();

            int objectiveConstant = 0;

            // 1. 偏好匹配项 - 教师和课程的匹配得分
            foreach (var section in problem.CourseSections)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    foreach (var classroom in problem.Classrooms)
                    {
                        // 计算教室类型与课程需求的匹配得分
                        int roomTypeScore = CalculateRoomTypeMatchScore(section, classroom, problem);

                        // 计算教室容量与课程人数的匹配得分
                        int capacityScore = CalculateCapacityScore(section.Enrollment, classroom.Capacity);

                        // 评估时间段偏好
                        int timeSlotScore = 10; // 默认分数
                        
                        // 移除对晚上时间段的特殊权重设置，使所有时间段具有相同的权重
                        // 不再区分时间段类型(早上、下午、晚上)，公平对待每个时间段

                        foreach (var teacher in problem.Teachers)
                        {
                            string varName = $"c{section.Id}_t{timeSlot.Id}_r{classroom.Id}_f{teacher.Id}";
                            if (_variables.TryGetValue(varName, out var variable))
                            {
                                // 计算教师对这门课程的偏好得分
                                int teacherPreferenceScore = 0;
                                var preference = problem.TeacherCoursePreferences
                                    .FirstOrDefault(tcp => tcp.TeacherId == teacher.Id && tcp.CourseId == section.CourseId);

                                if (preference != null)
                                {
                                    // 根据教师的专业水平和偏好计算得分
                                    teacherPreferenceScore = preference.ProficiencyLevel * 5 + preference.PreferenceLevel * 2;
                                }

                                // 将所有得分累加
                                int totalScore = teacherPreferenceScore + roomTypeScore + capacityScore + timeSlotScore;

                                terms.Add(variable);
                                coefficients.Add(totalScore);
                            }
                        }
                    }
                }
            }

            // 2. 添加约束偏好 - 教师工作量平衡等
            
            // 添加工作日平衡项等其他目标

            // 设置目标函数
            if (terms.Count > 0)
            {
                model.Maximize(LinearExpr.WeightedSum(terms.ToArray(), coefficients.ToArray()) + objectiveConstant);
            }
        }

        /// <summary>
        /// 计算课程与教室类型的匹配度
        /// </summary>
        private int CalculateRoomTypeMatchScore(CourseSectionInfo course, ClassroomInfo classroom, SchedulingProblem problem)
        {
            // 如果课程有教室类型需求，但是类型不匹配
            if (!string.IsNullOrEmpty(course.RequiredRoomType) && 
                !string.IsNullOrEmpty(classroom.RoomType) && 
                !IsCompatibleRoomType(course.RequiredRoomType, classroom.RoomType))
            {
                return 0; // 不匹配
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
        private void AddOneCourseOneAssignmentConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var course in problem.CourseSections)
            {
                Console.WriteLine("添加每门课程必须分配一次的约束");

                // 找出所有涉及此课程班级的变量
                var courseVars = _variables
                    .Where(kv => kv.Key.StartsWith($"c{course.Id}_"))
                    .Select(kv => kv.Value)
                    .ToList();
                Console.WriteLine($"课程 {course.Id} 找到 {courseVars.Count} 个变量");

                if (courseVars.Count > 0)
                {
                    // 约束：每门课程必须且只能分配一次
                    model.Add(LinearExpr.Sum(courseVars) == 1);
                    Console.WriteLine($"为课程 {course.Id} 添加了OneCourseOneAssignment约束");
                }
                else
                {
                    Console.WriteLine($"警告：课程 {course.Id} 没有找到相关变量，无法添加约束!");
                }
            }
        }

        private void AddTeacherConflictConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            _logger.LogDebug("添加教师冲突约束...");

            // 预处理：按教师和时间槽分组变量
            var teacherTimeVarsMap = new Dictionary<(int teacherId, int timeSlotId), List<IntVar>>();

            foreach (var entry in variables)
            {
                string key = entry.Key;

                // 解析变量名 "c{sectionId}_t{timeSlotId}_r{roomId}_f{teacherId}"
                var parts = key.Split('_');
                if (parts.Length < 4) continue;

                int timeSlotId = int.Parse(parts[1].Substring(1));
                int teacherId = int.Parse(parts[3].Substring(1));

                var mapKey = (teacherId, timeSlotId);
                if (!teacherTimeVarsMap.ContainsKey(mapKey))
                {
                    teacherTimeVarsMap[mapKey] = new List<IntVar>();
                }

                teacherTimeVarsMap[mapKey].Add(entry.Value);
            }

            // 批量添加约束 - 同一教师在同一时间段最多只能教一门课
            int constraintCount = 0;
            foreach (var entry in teacherTimeVarsMap)
            {
                var conflictingVars = entry.Value;
                if (conflictingVars.Count > 1)
                {
                    model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    constraintCount++;
                }
            }

            _logger.LogDebug($"添加了 {constraintCount} 个教师冲突约束");
        }

        private void AddClassroomConflictConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            _logger.LogDebug("添加教室冲突约束...");

            // 预处理：按教室和时间槽分组变量
            var roomTimeVarsMap = new Dictionary<(int roomId, int timeSlotId), List<IntVar>>();

            foreach (var entry in variables)
            {
                string key = entry.Key;

                // 解析变量名 "c{sectionId}_t{timeSlotId}_r{roomId}_f{teacherId}"
                var parts = key.Split('_');
                if (parts.Length < 4) continue;

                int timeSlotId = int.Parse(parts[1].Substring(1));
                int roomId = int.Parse(parts[2].Substring(1));

                var mapKey = (roomId, timeSlotId);
                if (!roomTimeVarsMap.ContainsKey(mapKey))
                {
                    roomTimeVarsMap[mapKey] = new List<IntVar>();
                }

                roomTimeVarsMap[mapKey].Add(entry.Value);
            }

            // 批量添加约束 - 同一教室在同一时间段最多只能安排一门课
            int constraintCount = 0;
            foreach (var entry in roomTimeVarsMap)
            {
                var conflictingVars = entry.Value;
                if (conflictingVars.Count > 1)
                {
                    model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    constraintCount++;
                }
            }

            _logger.LogDebug($"添加了 {constraintCount} 个教室冲突约束");
        }

        private void AddTeacherAvailabilityConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var availability in problem.TeacherAvailabilities)
            {
                if (!availability.IsAvailable)
                {
                    // 找出教师在不可用时间段的所有可能分配
                    var unavailableVars = _variables
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

        private void AddClassroomAvailabilityConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var availability in problem.ClassroomAvailabilities)
            {
                if (!availability.IsAvailable)
                {
                    // 找出教室在不可用时间段的所有可能分配
                    var unavailableVars = _variables
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

        private void AddClassroomCapacityConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
        {
            foreach (var section in problem.CourseSections)
            {
                foreach (var classroom in problem.Classrooms)
                {
                    if (classroom.Capacity < section.Enrollment)
                    {
                        // 找出教室容量不足的所有可能分配
                        var invalidVars = _variables
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

        private void AddPrerequisiteConstraints(CpModel model, Dictionary<string, IntVar> _variables, SchedulingProblem problem)
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
                                    var sectionTimeVars = _variables
                                        .Where(kv => kv.Key.StartsWith($"c{sectionId}_t{timeSlot.Id}_"))
                                        .Select(kv => kv.Value)
                                        .ToList();

                                    var prereqTimeVars = _variables
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