using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<CourseSection> CourseSections { get; set; }
        public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; }
        public DbSet<ClassroomAvailability> ClassroomAvailabilities { get; set; }
        public DbSet<ScheduleResult> ScheduleResults { get; set; }
        public DbSet<SchedulingConstraint> SchedulingConstraints { get; set; }
        public DbSet<AISchedulingSuggestion> AISchedulingSuggestions { get; set; }

        public DbSet<TeacherPreference> TeacherPreferences { get; set; }
        public DbSet<CampusTravelTime> CampusTravelTimes { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<UnavailablePeriod> UnavailablePeriods { get; set; }
        public DbSet<Prerequisite> Prerequisites { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost;Database=SmartSchedulingSystem;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Add semester data
            modelBuilder.Entity<Semester>().HasData(
                new Semester { SemesterId = 1, Name = "Spring 2024", StartDate = new DateTime(2024, 2, 15), EndDate = new DateTime(2024, 6, 30) },
                new Semester { SemesterId = 2, Name = "Fall 2024", StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2025, 1, 15) }
            );

            // Add department data
            modelBuilder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, Name = "Computer Science", Code = "CS" },
                new Department { DepartmentId = 2, Name = "Mathematics", Code = "MATH" },
                new Department { DepartmentId = 3, Name = "Physics", Code = "PHYS" }
            );

            // Add teacher data with phone numbers
            modelBuilder.Entity<Teacher>().HasData(
                new Teacher { TeacherId = 1, Name = "Prof. Zhang", Code = "T001", DepartmentId = 1, Email = "zhang@university.edu", PhoneNumber = "555-1001" },
                new Teacher { TeacherId = 2, Name = "Prof. Li", Code = "T002", DepartmentId = 1, Email = "li@university.edu", PhoneNumber = "555-1002" },
                new Teacher { TeacherId = 3, Name = "Prof. Wang", Code = "T003", DepartmentId = 2, Email = "wang@university.edu", PhoneNumber = "555-1003" },
                new Teacher { TeacherId = 4, Name = "Prof. Zhao", Code = "T004", DepartmentId = 3, Email = "zhao@university.edu", PhoneNumber = "555-1004" }
            );

            // Add course data
            modelBuilder.Entity<Course>().HasData(
                new Course { CourseId = 1, Name = "Object-Oriented Programming", Code = "CS101", Credits = 4, WeeklyHours = 4, DepartmentId = 1 },
                new Course { CourseId = 2, Name = "Data Structures", Code = "CS102", Credits = 4, WeeklyHours = 4, DepartmentId = 1 },
                new Course { CourseId = 3, Name = "Discrete Mathematics", Code = "MATH101", Credits = 3, WeeklyHours = 3, DepartmentId = 2 },
                new Course { CourseId = 4, Name = "Introduction to Quantum Mechanics", Code = "PHYS101", Credits = 4, WeeklyHours = 4, DepartmentId = 3 }
            );

            // Add classroom data
            modelBuilder.Entity<Classroom>().HasData(
                new Classroom { ClassroomId = 1, Name = "Building A-101", Building = "Building A", Capacity = 50},
                new Classroom { ClassroomId = 2, Name = "Building A-202", Building = "Building A", Capacity = 40},
                new Classroom { ClassroomId = 3, Name = "Building B-305", Building = "Building B", Capacity = 60 }
            );

            // Add time slot data
            modelBuilder.Entity<TimeSlot>().HasData(
                new TimeSlot { TimeSlotId = 1, DayOfWeek = 1, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0) },
                new TimeSlot { TimeSlotId = 2, DayOfWeek = 1, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0) },
                new TimeSlot { TimeSlotId = 3, DayOfWeek = 2, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0) },
                new TimeSlot { TimeSlotId = 4, DayOfWeek = 2, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0) }
            );

            // Add course section data
            modelBuilder.Entity<CourseSection>().HasData(
                new CourseSection
                {
                    CourseSectionId = 1,
                    CourseId = 1,
                    SemesterId = 1,
                    SectionCode = "CS101-01",
                    MaxEnrollment = 50,
                    ActualEnrollment = 0
                },
                new CourseSection
                {
                    CourseSectionId = 2,
                    CourseId = 2,
                    SemesterId = 1,
                    SectionCode = "CS102-01",
                    MaxEnrollment = 40,
                    ActualEnrollment = 0
                }
            );

            // Add teacher availability data
            modelBuilder.Entity<TeacherAvailability>().HasData(
                new TeacherAvailability { TeacherId = 1, TimeSlotId = 1, IsAvailable = true },
                new TeacherAvailability { TeacherId = 1, TimeSlotId = 2, IsAvailable = true },
                new TeacherAvailability { TeacherId = 2, TimeSlotId = 3, IsAvailable = true },
                new TeacherAvailability { TeacherId = 2, TimeSlotId = 4, IsAvailable = true }
            );

            // Add classroom availability data
            modelBuilder.Entity<ClassroomAvailability>().HasData(
                new ClassroomAvailability { ClassroomId = 1, TimeSlotId = 1, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 1, TimeSlotId = 2, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 2, TimeSlotId = 3, IsAvailable = true },
                new ClassroomAvailability { ClassroomId = 2, TimeSlotId = 4, IsAvailable = true }
            );

            // Add scheduling constraint data
            modelBuilder.Entity<SchedulingConstraint>().HasData(
                new SchedulingConstraint
                {
                    ConstraintId = 1,
                    ConstraintType = "Teacher",
                    ConstraintName = "Teacher Availability",
                    ConstraintDescription = "Ensure teachers are available during scheduled time slots",
                    Weight = 1.0,
                    IsActive = true
                },
                new SchedulingConstraint
                {
                    ConstraintId = 2,
                    ConstraintType = "Classroom",
                    ConstraintName = "Classroom Capacity",
                    ConstraintDescription = "Ensure classroom capacity meets course requirements",
                    Weight = 1.0,
                    IsActive = true
                }
            );
        }
        // In the OnModelCreating method of AppDbContext
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Call the seed data method
            SeedData(modelBuilder);

            // Configure composite keys
            // Add missing primary key configurations
            modelBuilder.Entity<Department>()
                .HasKey(d => d.DepartmentId);

            modelBuilder.Entity<Teacher>()
                .HasKey(t => t.TeacherId);

            modelBuilder.Entity<Course>()
                .HasKey(c => c.CourseId);

            modelBuilder.Entity<Classroom>()
                .HasKey(c => c.ClassroomId);

            modelBuilder.Entity<Semester>()
                .HasKey(s => s.SemesterId);

            modelBuilder.Entity<TimeSlot>()
                .HasKey(ts => ts.TimeSlotId);

            modelBuilder.Entity<CourseSection>()
                .HasKey(cs => cs.CourseSectionId);

            modelBuilder.Entity<SchedulingConstraint>()
                .HasKey(sc => sc.ConstraintId);
            modelBuilder.Entity<ScheduleResult>()
                .HasKey(sc => sc.ScheduleId);

            modelBuilder.Entity<TeacherAvailability>()
                .HasKey(ta => new { ta.TeacherId, ta.TimeSlotId });

            modelBuilder.Entity<ClassroomAvailability>()
                .HasKey(ca => new { ca.ClassroomId, ca.TimeSlotId });
            modelBuilder.Entity<CampusTravelTime>()
                .HasKey(c => new { c.FromCampusId, c.ToCampusId });

            modelBuilder.Entity<Prerequisite>()
                .HasKey(p => new { p.CourseId, p.PrerequisiteCourseId });

            // Configure relationships
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Department)
                .WithMany(d => d.Teachers)
                .HasForeignKey(t => t.DepartmentId);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Department)
                .WithMany(d => d.Courses)
                .HasForeignKey(c => c.DepartmentId);

            modelBuilder.Entity<CourseSection>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.CourseSections)
                .HasForeignKey(cs => cs.CourseId);

            modelBuilder.Entity<CourseSection>()
                .HasOne(cs => cs.Semester)
                .WithMany(s => s.CourseSections)
                .HasForeignKey(cs => cs.SemesterId);

            modelBuilder.Entity<TeacherAvailability>()
                .HasOne(ta => ta.Teacher)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(ta => ta.TeacherId);

            modelBuilder.Entity<TeacherAvailability>()
                .HasOne(ta => ta.TimeSlot)
                .WithMany(ts => ts.TeacherAvailabilities)
                .HasForeignKey(ta => ta.TimeSlotId);

            modelBuilder.Entity<ClassroomAvailability>()
                .HasOne(ca => ca.Classroom)
                .WithMany(c => c.Availabilities)
                .HasForeignKey(ca => ca.ClassroomId);

            modelBuilder.Entity<ClassroomAvailability>()
                .HasOne(ca => ca.TimeSlot)
                .WithMany(ts => ts.ClassroomAvailabilities)
                .HasForeignKey(ca => ca.TimeSlotId);
            modelBuilder.Entity<ScheduleResult>()
               .HasOne(sr => sr.Semester)
               .WithMany(s => s.ScheduleResults)
               .HasForeignKey(sr => sr.SemesterId)
               .OnDelete(DeleteBehavior.Restrict);
         

            modelBuilder.Entity<TeacherPreference>()
                .HasKey(p => new { p.TeacherId, p.TimeSlotId });

            // Update the relationship configuration for the ScheduleItem entity
            modelBuilder.Entity<ScheduleItem>(entity =>
            {
                // Modify the foreign key relationship for ScheduleResult
                entity.HasOne(si => si.ScheduleResult)
                    .WithMany(sr => sr.Items)
                    .HasForeignKey(si => si.ScheduleResultId)
                    .OnDelete(DeleteBehavior.Restrict); // 改为 Restrict

                // CourseSection foreign key
                entity.HasOne(si => si.CourseSection)
                    .WithMany()
                    .HasForeignKey(si => si.CourseSectionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Teacher foreign key
                entity.HasOne(si => si.Teacher)
                    .WithMany()
                    .HasForeignKey(si => si.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Classroom foreign key
                entity.HasOne(si => si.Classroom)
                    .WithMany()
                    .HasForeignKey(si => si.ClassroomId)
                    .OnDelete(DeleteBehavior.Restrict);

                // TimeSlot foreign key
                entity.HasOne(si => si.TimeSlot)
                    .WithMany()
                    .HasForeignKey(si => si.TimeSlotId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            // You can keep the cascading delete for CourseSection and TimeSlot
            // Or also change them to NoAction based on your business needs

            // Add AISchedulingSuggestion configuration
            modelBuilder.Entity<AISchedulingSuggestion>(entity =>
            {
                // Specify the primary key
                entity.HasKey(x => x.SuggestionId);

                // Optional: Add an index
                entity.HasIndex(x => x.ScheduleRequestId);

                // Configure fields
                entity.Property(x => x.SuggestionData)
                    .HasColumnType("nvarchar(max)");

                // Set the default creation time
                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");
            });
        }
    }
}