using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Interfaces;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// 全局约束管理器访问点，用于在不同组件之间共享约束管理器
    /// </summary>
    public static class GlobalConstraintManager
    {
        private static ConstraintManager _current;

        /// <summary>
        /// 获取或设置当前约束管理器实例
        /// </summary>
        public static IConstraintManager Current
        {
            get { return _current; }
            set { _current = value as ConstraintManager; }
        }

        /// <summary>
        /// 初始化全局约束管理器
        /// </summary>
        public static void Initialize(ConstraintManager constraintManager)
        {
            _current = constraintManager;
            
            // 初始化时设置为最小约束级别，以确保能够找到初始解
            _current?.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
        }

        /// <summary>
        /// 清除当前约束管理器实例
        /// </summary>
        public static void ClearCurrent()
        {
            Current = null;
        }
    }
} 