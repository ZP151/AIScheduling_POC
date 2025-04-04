namespace SmartSchedulingSystem.Core.DTOs
{
    public class CoursePrerequisiteDto
    {
        public int PrerequisiteId { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int PrerequisiteCourseId { get; set; }
        public string PrerequisiteCourseCode { get; set; }
        public string PrerequisiteCourseName { get; set; }
        public bool IsRequired { get; set; }
        public double MinimumGrade { get; set; }
    }
}