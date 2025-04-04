using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartSchedulingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorScheduleStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AISchedulingSuggestions",
                columns: table => new
                {
                    SuggestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleRequestId = table.Column<int>(type: "int", nullable: false),
                    SuggestionData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISchedulingSuggestions", x => x.SuggestionId);
                });

            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    ClassroomId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Building = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    HasComputers = table.Column<bool>(type: "bit", nullable: false),
                    HasProjector = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.ClassroomId);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "SchedulingConstraints",
                columns: table => new
                {
                    ConstraintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConstraintType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConstraintName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConstraintDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulingConstraints", x => x.ConstraintId);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    SemesterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.SemesterId);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    TimeSlotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.TimeSlotId);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Credits = table.Column<int>(type: "int", nullable: false),
                    WeeklyHours = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseId);
                    table.ForeignKey(
                        name: "FK_Courses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.TeacherId);
                    table.ForeignKey(
                        name: "FK_Teachers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomAvailabilities",
                columns: table => new
                {
                    ClassroomId = table.Column<int>(type: "int", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomAvailabilities", x => new { x.ClassroomId, x.TimeSlotId });
                    table.ForeignKey(
                        name: "FK_ClassroomAvailabilities_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "ClassroomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassroomAvailabilities_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "TimeSlotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseSections",
                columns: table => new
                {
                    CourseSectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    SectionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxEnrollment = table.Column<int>(type: "int", nullable: false),
                    ActualEnrollment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSections", x => x.CourseSectionId);
                    table.ForeignKey(
                        name: "FK_CourseSections_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSections_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "SemesterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAvailabilities",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAvailabilities", x => new { x.TeacherId, x.TimeSlotId });
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilities_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "TeacherId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilities_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "TimeSlotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleResults",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    ClassroomId = table.Column<int>(type: "int", nullable: true),
                    CourseSectionId = table.Column<int>(type: "int", nullable: true),
                    TeacherId = table.Column<int>(type: "int", nullable: true),
                    TimeSlotId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleResults", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_ScheduleResults_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "ClassroomId");
                    table.ForeignKey(
                        name: "FK_ScheduleResults_CourseSections_CourseSectionId",
                        column: x => x.CourseSectionId,
                        principalTable: "CourseSections",
                        principalColumn: "CourseSectionId");
                    table.ForeignKey(
                        name: "FK_ScheduleResults_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "SemesterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleResults_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "TeacherId");
                    table.ForeignKey(
                        name: "FK_ScheduleResults_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "TimeSlotId");
                });

            migrationBuilder.CreateTable(
                name: "ScheduleItem",
                columns: table => new
                {
                    ScheduleItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleResultId = table.Column<int>(type: "int", nullable: false),
                    CourseSectionId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ClassroomId = table.Column<int>(type: "int", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleItem", x => x.ScheduleItemId);
                    table.ForeignKey(
                        name: "FK_ScheduleItem_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "ClassroomId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleItem_CourseSections_CourseSectionId",
                        column: x => x.CourseSectionId,
                        principalTable: "CourseSections",
                        principalColumn: "CourseSectionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleItem_ScheduleResults_ScheduleResultId",
                        column: x => x.ScheduleResultId,
                        principalTable: "ScheduleResults",
                        principalColumn: "ScheduleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleItem_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "TeacherId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleItem_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "TimeSlotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Classrooms",
                columns: new[] { "ClassroomId", "Building", "Capacity", "HasComputers", "HasProjector", "Name" },
                values: new object[,]
                {
                    { 1, "Building A", 50, true, true, "Building A-101" },
                    { 2, "Building A", 40, false, true, "Building A-202" },
                    { 3, "Building B", 60, true, true, "Building B-305" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "CS", "Computer Science" },
                    { 2, "MATH", "Mathematics" },
                    { 3, "PHYS", "Physics" }
                });

            migrationBuilder.InsertData(
                table: "SchedulingConstraints",
                columns: new[] { "ConstraintId", "ConstraintDescription", "ConstraintName", "ConstraintType", "IsActive", "Weight" },
                values: new object[,]
                {
                    { 1, "Ensure teachers are available during scheduled time slots", "Teacher Availability", "Teacher", true, 1.0 },
                    { 2, "Ensure classroom capacity meets course requirements", "Classroom Capacity", "Classroom", true, 1.0 }
                });

            migrationBuilder.InsertData(
                table: "Semesters",
                columns: new[] { "SemesterId", "EndDate", "Name", "StartDate" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Spring 2024", new DateTime(2024, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, new DateTime(2025, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fall 2024", new DateTime(2024, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "TimeSlots",
                columns: new[] { "TimeSlotId", "DayOfWeek", "EndTime", "StartTime" },
                values: new object[,]
                {
                    { 1, 1, new TimeSpan(0, 9, 30, 0, 0), new TimeSpan(0, 8, 0, 0, 0) },
                    { 2, 1, new TimeSpan(0, 11, 30, 0, 0), new TimeSpan(0, 10, 0, 0, 0) },
                    { 3, 2, new TimeSpan(0, 9, 30, 0, 0), new TimeSpan(0, 8, 0, 0, 0) },
                    { 4, 2, new TimeSpan(0, 11, 30, 0, 0), new TimeSpan(0, 10, 0, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "ClassroomAvailabilities",
                columns: new[] { "ClassroomId", "TimeSlotId", "IsAvailable" },
                values: new object[,]
                {
                    { 1, 1, true },
                    { 1, 2, true },
                    { 2, 3, true },
                    { 2, 4, true }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "CourseId", "Code", "Credits", "DepartmentId", "Name", "WeeklyHours" },
                values: new object[,]
                {
                    { 1, "CS101", 4, 1, "Object-Oriented Programming", 4 },
                    { 2, "CS102", 4, 1, "Data Structures", 4 },
                    { 3, "MATH101", 3, 2, "Discrete Mathematics", 3 },
                    { 4, "PHYS101", 4, 3, "Introduction to Quantum Mechanics", 4 }
                });

            migrationBuilder.InsertData(
                table: "Teachers",
                columns: new[] { "TeacherId", "Code", "DepartmentId", "Email", "Name", "PhoneNumber" },
                values: new object[,]
                {
                    { 1, "T001", 1, "zhang@university.edu", "Prof. Zhang", "555-1001" },
                    { 2, "T002", 1, "li@university.edu", "Prof. Li", "555-1002" },
                    { 3, "T003", 2, "wang@university.edu", "Prof. Wang", "555-1003" },
                    { 4, "T004", 3, "zhao@university.edu", "Prof. Zhao", "555-1004" }
                });

            migrationBuilder.InsertData(
                table: "CourseSections",
                columns: new[] { "CourseSectionId", "ActualEnrollment", "CourseId", "MaxEnrollment", "SectionCode", "SemesterId" },
                values: new object[,]
                {
                    { 1, 0, 1, 50, "CS101-01", 1 },
                    { 2, 0, 2, 40, "CS102-01", 1 }
                });

            migrationBuilder.InsertData(
                table: "TeacherAvailabilities",
                columns: new[] { "TeacherId", "TimeSlotId", "IsAvailable" },
                values: new object[,]
                {
                    { 1, 1, true },
                    { 1, 2, true },
                    { 2, 3, true },
                    { 2, 4, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AISchedulingSuggestions_ScheduleRequestId",
                table: "AISchedulingSuggestions",
                column: "ScheduleRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomAvailabilities_TimeSlotId",
                table: "ClassroomAvailabilities",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSections_CourseId",
                table: "CourseSections",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSections_SemesterId",
                table: "CourseSections",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItem_ClassroomId",
                table: "ScheduleItem",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItem_CourseSectionId",
                table: "ScheduleItem",
                column: "CourseSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItem_ScheduleResultId",
                table: "ScheduleItem",
                column: "ScheduleResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItem_TeacherId",
                table: "ScheduleItem",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItem_TimeSlotId",
                table: "ScheduleItem",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleResults_ClassroomId",
                table: "ScheduleResults",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleResults_CourseSectionId",
                table: "ScheduleResults",
                column: "CourseSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleResults_SemesterId",
                table: "ScheduleResults",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleResults_TeacherId",
                table: "ScheduleResults",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleResults_TimeSlotId",
                table: "ScheduleResults",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAvailabilities_TimeSlotId",
                table: "TeacherAvailabilities",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_DepartmentId",
                table: "Teachers",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AISchedulingSuggestions");

            migrationBuilder.DropTable(
                name: "ClassroomAvailabilities");

            migrationBuilder.DropTable(
                name: "ScheduleItem");

            migrationBuilder.DropTable(
                name: "SchedulingConstraints");

            migrationBuilder.DropTable(
                name: "TeacherAvailabilities");

            migrationBuilder.DropTable(
                name: "ScheduleResults");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "CourseSections");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
