using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 构建CP模型的工具类
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
            var model = new CpModel();
            var variables = CreateDecisionVariables(model, problem);

            // 应用所有约束转换器
            foreach (var converter in _constraintConverters)
            {
                converter.AddToModel(model, variables, problem);
            }

            return model;
        }

        /// <summary>
        /// 创建决策变量
        /// </summary>
        private Dictionary<string, IntVar> CreateDecisionVariables(CpModel model, SchedulingProblem problem)
        {
            var variables = new Dictionary<string, IntVar>();

            // 为每个课程-时间-教室-教师的可能组合创建二元变量
            foreach (var course in problem.CourseSections)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    foreach (var classroom in problem.Classrooms)
                    {
                        foreach (var teacher in problem.Teachers)
                        {
                            // 创建唯一标识符
                            string varName = $"c{course.Id}_t{timeSlot.Id}_r{classroom.Id}_f{teacher.Id}";

                            // 创建0-1整数变量(0=不分配，1=分配)
                            var variable = model.NewBoolVar(varName);
                            variables[varName] = variable;
                        }
                    }
                }
            }

            // 为每个课程创建表示是否已分配的辅助变量
            foreach (var course in problem.CourseSections)
            {
                string varName = $"assigned_c{course.Id}";
                var variable = model.NewBoolVar(varName);
                variables[varName] = variable;
            }

            return variables;
        }

        /// <summary>
        /// 添加每门课程必须且只能分配一次的约束
        /// </summary>
        private void AddCourseAssignmentConstraints(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var course in problem.CourseSections)
            {
                // 收集与此课程相关的所有分配变量
                var courseVars = variables
                    .Where(kv => kv.Key.StartsWith($"c{course.Id}_"))
                    .Select(kv => kv.Value)
                    .ToList();

                // 确保每门课程恰好分配一次
                model.Add(LinearExpr.Sum(courseVars) == 1);
            }
        }
    }
}