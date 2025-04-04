using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Engine.LS.Moves
{
    /// <summary>
    /// 表示局部搜索中的移动操作接口
    /// </summary>
    public interface IMove
    {
        /// <summary>
        /// 应用移动到指定解决方案
        /// </summary>
        /// <param name="solution">要修改的解决方案</param>
        /// <returns>修改后的解决方案的副本</returns>
        SchedulingSolution Apply(SchedulingSolution solution);

        /// <summary>
        /// 获取移动的描述
        /// </summary>
        string GetDescription();

        /// <summary>
        /// 获取此移动涉及的课程分配ID
        /// </summary>
        int[] GetAffectedAssignmentIds();
    }
}