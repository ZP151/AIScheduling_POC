using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// 将教师课程偏好约束转换为CP模型约束
    /// </summary>
    public class TeacherCoursePreferenceConstraintConverter : ICPConstraintConverter
    {
        private readonly Dictionary<(int TeacherId, int CourseId), bool> _teacherCoursePreferences;
        private readonly Dictionary<int, int> _courseSectionMap; // 班级ID -> 课程ID

        public TeacherCoursePreferenceConstraintConverter(
            Dictionary<(int TeacherId, int CourseId), bool> teacherCoursePreferences,
            Dictionary<int, int> courseSectionMap)
        {
            _teacherCoursePreferences = teacherCoursePreferences ?? throw new ArgumentNullException(nameof(teacherCoursePreferences));
            _courseSectionMap = courseSectionMap ?? throw new ArgumentNullException(nameof(courseSectionMap));
        }

        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // 处理每个教师
            foreach (var teacher in problem.Teachers)
            {
                // 处理每个班级
                foreach (var section in problem.CourseSections)
                {
                    // 获取班级对应的课程ID
                    if (!_courseSectionMap.TryGetValue(section.Id, out int courseId))
                    {
                        courseId = section.CourseId; // 使用班级的默认课程ID
                    }

                    // 检查教师是否可以教授该课程
                    if (_teacherCoursePreferences.TryGetValue((teacher.Id, courseId), out bool canTeach) && !canTeach)
                    {
                        // 找出所有将该教师安排给该班级的变量
                        var invalidVars = variables
                            .Where(kv => kv.Key.StartsWith($"c{section.Id}_") &&
                                       kv.Key.Contains($"_i{teacher.Id}"))
                            .Select(kv => kv.Value)
                            .ToList();

                        // 添加约束：这些变量必须为0（禁止该教师教授该班级）
                        foreach (var variable in invalidVars)
                        {
                            model.Add(variable == 0);
                        }
                    }
                }
            }
        }
    }
} 