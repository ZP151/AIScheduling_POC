using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Scheduling.Interfaces
{
    /// <summary>
    /// 约束管理器接口
    /// </summary>
    public interface IConstraintManager
    {
        #region 约束管理方法

        /// <summary>
        /// 注册约束
        /// </summary>
        /// <param name="constraint">要注册的约束</param>
        void RegisterConstraint(IConstraint constraint);

        /// <summary>
        /// 批量注册约束
        /// </summary>
        /// <param name="constraints">要注册的约束集合</param>
        void RegisterConstraints(IEnumerable<IConstraint> constraints);

        /// <summary>
        /// 添加约束
        /// </summary>
        /// <param name="constraint">要添加的约束</param>
        void AddConstraint(IConstraint constraint);

        /// <summary>
        /// 移除约束
        /// </summary>
        /// <param name="id">约束ID</param>
        void RemoveConstraint(string id);

        /// <summary>
        /// 停用约束
        /// </summary>
        /// <param name="constraintId">约束ID</param>
        void DeactivateConstraint(int constraintId);

        /// <summary>
        /// 启用约束
        /// </summary>
        /// <param name="constraintId">约束ID</param>
        void ActivateConstraint(int constraintId);

        /// <summary>
        /// 更新约束权重
        /// </summary>
        /// <param name="constraintId">约束ID</param>
        /// <param name="weight">新权重</param>
        void UpdateConstraintWeight(int constraintId, double weight);

        /// <summary>
        /// 启用或禁用简化约束集合
        /// </summary>
        /// <param name="useSimplified">是否使用简化约束集合</param>
        void UseSimplifiedConstraints(bool useSimplified = true);

        /// <summary>
        /// 设置约束应用级别
        /// </summary>
        /// <param name="level">约束应用级别</param>
        void SetConstraintApplicationLevel(ConstraintApplicationLevel level);

        /// <summary>
        /// 获取当前约束应用级别
        /// </summary>
        /// <returns>约束应用级别</returns>
        ConstraintApplicationLevel GetCurrentApplicationLevel();

        /// <summary>
        /// 加载约束配置
        /// </summary>
        /// <param name="constraintIds">要加载的约束ID列表</param>
        /// <param name="parameters">排课参数</param>
        void LoadConstraintConfiguration(List<string> constraintIds, SchedulingParameters parameters);

        #endregion

        #region 约束查询方法

        /// <summary>
        /// 获取所有约束
        /// </summary>
        /// <returns>约束列表</returns>
        List<IConstraint> GetAllConstraints();

        /// <summary>
        /// 获取所有硬约束
        /// </summary>
        /// <returns>硬约束列表</returns>
        List<IConstraint> GetHardConstraints();

        /// <summary>
        /// 获取所有软约束
        /// </summary>
        /// <returns>软约束列表</returns>
        List<IConstraint> GetSoftConstraints();

        /// <summary>
        /// 通过数字ID查找约束
        /// </summary>
        /// <param name="id">约束ID</param>
        /// <returns>约束</returns>
        IConstraint FindConstraint(int id);

        /// <summary>
        /// 通过字符串ID获取约束
        /// </summary>
        /// <param name="id">约束ID</param>
        /// <returns>约束</returns>
        IConstraint GetConstraintById(string id);

        /// <summary>
        /// 通过定义ID查找约束
        /// </summary>
        /// <param name="definitionId">定义ID</param>
        /// <returns>约束</returns>
        IConstraint FindConstraintByDefinitionId(string definitionId);

        /// <summary>
        /// 按基本规则获取约束
        /// </summary>
        /// <param name="basicRule">基本规则</param>
        /// <returns>满足基本规则的约束列表</returns>
        List<IConstraint> GetConstraintsByBasicRule(string basicRule);

        /// <summary>
        /// 按层级获取约束
        /// </summary>
        /// <param name="hierarchy">约束层级</param>
        /// <returns>满足层级的约束列表</returns>
        List<IConstraint> GetConstraintsByHierarchy(ConstraintHierarchy hierarchy);

        /// <summary>
        /// 获取活跃的约束
        /// </summary>
        /// <param name="level">约束应用级别</param>
        /// <returns>活跃约束列表</returns>
        List<IConstraint> GetActiveConstraints(ConstraintApplicationLevel level);

        #endregion

        #region 约束评估方法

        /// <summary>
        /// 评估所有约束
        /// </summary>
        /// <param name="solution">排课解决方案</param>
        /// <returns>评估结果</returns>
        SchedulingEvaluation EvaluateConstraints(SchedulingSolution solution);

        /// <summary>
        /// 评估硬约束
        /// </summary>
        /// <param name="solution">排课解决方案</param>
        /// <returns>评估结果</returns>
        List<ConstraintEvaluation> EvaluateHardConstraints(SchedulingSolution solution);

        /// <summary>
        /// 评估软约束
        /// </summary>
        /// <param name="solution">排课解决方案</param>
        /// <returns>评估结果</returns>
        List<ConstraintEvaluation> EvaluateSoftConstraints(SchedulingSolution solution);

        /// <summary>
        /// 计算冲突
        /// </summary>
        /// <param name="solution">排课解决方案</param>
        /// <returns>冲突列表</returns>
        List<SchedulingConflict> CalculateConflicts(SchedulingSolution solution);

        #endregion
    }
} 