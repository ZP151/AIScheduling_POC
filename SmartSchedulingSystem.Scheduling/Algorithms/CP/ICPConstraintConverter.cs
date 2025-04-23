using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 约束转换接口，用于将Domain约束转换为CP模型约束
    /// </summary>
    public interface ICPConstraintConverter
    {
        /// <summary>
        /// 获取此约束转换器对应的约束级别
        /// </summary>
        ConstraintApplicationLevel ConstraintLevel { get; }

        /// <summary>
        /// 将约束添加到CP模型中
        /// </summary>
        /// <param name="model">CP模型</param>
        /// <param name="variables">变量字典</param>
        /// <param name="problem">排课问题</param>
        void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem);
    }
}
