using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// 表示排课问题的一个解决方案
    /// </summary>
    public class SchedulingSolution
    {
        /// <summary>
        /// 解决方案的唯一ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 所属排课问题的ID
        /// </summary>
        public int ProblemId { get; set; }
        public SchedulingProblem Problem { get; set; }

        public int? SolutionSetId { get; set; }
        public SchedulingEvaluation Evaluation { get; set; } // optional

        /// <summary>
        /// 解决方案的得分，直接返回Evaluation.Score
        /// </summary>
        public double Score => Evaluation?.Score ?? 0;

        /// <summary>
        /// 解决方案名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 排课分配列表
        /// </summary>
        public List<SchedulingAssignment> Assignments { get; set; } = new List<SchedulingAssignment>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建此解决方案的算法
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// 添加属性来跟踪解是在哪个约束级别下生成的
        /// </summary>
        public Engine.ConstraintApplicationLevel ConstraintLevel { get; set; } = Engine.ConstraintApplicationLevel.Basic;

        /// <summary>
        /// 获取特定课程班级的排课分配
        /// </summary>
        /// <param name="sectionId">课程班级ID</param>
        /// <returns>排课分配列表</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForSection(int sectionId)
        {
            return Assignments.Where(a => a.SectionId == sectionId);
        }

        /// <summary>
        /// 获取特定教师的排课分配
        /// </summary>
        /// <param name="teacherId">教师ID</param>
        /// <returns>排课分配列表</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForTeacher(int teacherId)
        {
            return Assignments.Where(a => a.TeacherId == teacherId);
        }

        /// <summary>
        /// 获取特定教室的排课分配
        /// </summary>
        /// <param name="classroomId">教室ID</param>
        /// <returns>排课分配列表</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForClassroom(int classroomId)
        {
            return Assignments.Where(a => a.ClassroomId == classroomId);
        }

        /// <summary>
        /// 获取特定时间槽的排课分配
        /// </summary>
        /// <param name="timeSlotId">时间槽ID</param>
        /// <returns>排课分配列表</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForTimeSlot(int timeSlotId)
        {
            return Assignments.Where(a => a.TimeSlotId == timeSlotId);
        }
        public IEnumerable<SchedulingAssignment> GetAssignmentsForDay(int dayOfWeek, List<TimeSlotInfo> timeSlots)
        {
            var relevantTimeSlots = timeSlots.Where(ts => ts.DayOfWeek == dayOfWeek).Select(ts => ts.Id).ToList();
            return Assignments.Where(a => relevantTimeSlots.Contains(a.TimeSlotId));
        }
        /// <summary>
        /// 检查指定时间槽是否有教师冲突
        /// </summary>
        /// <param name="teacherId">教师ID</param>
        /// <param name="timeSlotId">时间槽ID</param>
        /// <param name="ignoreSectionId">忽略的课程班级ID（可选）</param>
        /// <returns>是否存在冲突</returns>
        public bool HasTeacherConflict(int teacherId, int timeSlotId, int? ignoreSectionId = null)
        {
            return Assignments.Any(a =>
                a.TeacherId == teacherId &&
                a.TimeSlotId == timeSlotId &&
                (!ignoreSectionId.HasValue || a.SectionId != ignoreSectionId.Value));
        }

        /// <summary>
        /// 检查指定时间槽是否有教室冲突
        /// </summary>
        /// <param name="classroomId">教室ID</param>
        /// <param name="timeSlotId">时间槽ID</param>
        /// <param name="ignoreSectionId">忽略的课程班级ID（可选）</param>
        /// <returns>是否存在冲突</returns>
        public bool HasClassroomConflict(int classroomId, int timeSlotId, int? ignoreSectionId = null)
        {
            return Assignments.Any(a =>
                a.ClassroomId == classroomId &&
                a.TimeSlotId == timeSlotId &&
                (!ignoreSectionId.HasValue || a.SectionId != ignoreSectionId.Value));
        }

        /// <summary>
        /// 添加排课分配
        /// </summary>
        /// <param name="assignment">排课分配</param>
        /// <returns>是否添加成功</returns>
        public bool AddAssignment(SchedulingAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment));

            // 检查冲突
            if (HasTeacherConflict(assignment.TeacherId, assignment.TimeSlotId) ||
                HasClassroomConflict(assignment.ClassroomId, assignment.TimeSlotId))
            {
                return false;
            }

            Assignments.Add(assignment);
            return true;
        }

        /// <summary>
        /// 移除排课分配
        /// </summary>
        /// <param name="assignmentId">排课分配ID</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveAssignment(int assignmentId)
        {
            var assignment = Assignments.FirstOrDefault(a => a.Id == assignmentId);
            if (assignment != null)
            {
                return Assignments.Remove(assignment);
            }

            return false;
        }

        /// <summary>
        /// 更新排课分配
        /// </summary>
        /// <param name="assignment">更新后的排课分配</param>
        /// <returns>是否更新成功</returns>
        public bool UpdateAssignment(SchedulingAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment));

            int index = Assignments.FindIndex(a => a.Id == assignment.Id);
            if (index >= 0)
            {
                // 先移除旧的分配
                Assignments.RemoveAt(index);

                // 检查新分配是否会导致冲突
                if (HasTeacherConflict(assignment.TeacherId, assignment.TimeSlotId, assignment.SectionId) ||
                    HasClassroomConflict(assignment.ClassroomId, assignment.TimeSlotId, assignment.SectionId))
                {
                    // 冲突，恢复原来的分配
                    Assignments.Insert(index, assignment);
                    return false;
                }

                // 无冲突，添加新分配
                Assignments.Add(assignment);
                return true;
            }

            return false;
        }
        /// <summary>
        /// 创建解决方案的深拷贝
        /// </summary>
        public SchedulingSolution Clone()
        {
            var clone = new SchedulingSolution
            {
                Id = this.Id,
                ProblemId = this.ProblemId,
                Problem = this.Problem,
                SolutionSetId = this.SolutionSetId,
                Name = this.Name,
                CreatedAt = this.CreatedAt,
                Algorithm = this.Algorithm
            };

            // 深拷贝所有分配
            clone.Assignments = Assignments.Select(a => new SchedulingAssignment
            {
                Id = a.Id,
                SectionId = a.SectionId,
                SectionCode = a.SectionCode,
                TeacherId = a.TeacherId,
                TeacherName = a.TeacherName,
                ClassroomId = a.ClassroomId,
                ClassroomName = a.ClassroomName,
                TimeSlotId = a.TimeSlotId,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                WeekPattern = a.WeekPattern != null ? new List<int>(a.WeekPattern) : new List<int>()
            }).ToList();

            return clone;
        }

        /// <summary>
        /// 获取下一个冲突ID
        /// </summary>
        public int GetNextConflictId()
        {
            // 如果评估对象存在，计算现有冲突的最大ID并加1
            if (Evaluation != null && Evaluation.Conflicts != null && Evaluation.Conflicts.Any())
            {
                return Evaluation.Conflicts.Max(c => c.Id) + 1;
            }
            
            // 否则从1开始
            return 1;
        }
    }

    
}