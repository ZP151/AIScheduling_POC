using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 定义约束到CP模型转换的接口
    /// </summary>
    public interface ICPConstraintConverter
    {
        /// <summary>
        /// 将约束添加到CP模型
        /// </summary>
        /// <param name="model">CP求解器模型</param>
        /// <param name="variables">决策变量字典</param>
        /// <param name="problem">排课问题</param>
        void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem);
    }
}
