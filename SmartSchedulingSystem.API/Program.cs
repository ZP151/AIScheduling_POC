using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchedulSmartSchedulingSystemingSystem.Scheduling.Constraints.Soft;
using SmartSchedulingSystem.API.Middleware;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Core.Services;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Constraints.Hard;
using SmartSchedulingSystem.Scheduling.Constraints.PhysicalSoft;
using SmartSchedulingSystem.Scheduling.Constraints.QualitySoft;
using SmartSchedulingSystem.Scheduling.Constraints.Soft;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Utils;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// ��ӷ�������
builder.Services.AddControllers();

// ����Swagger
builder.Services.AddEndpointsApiExplorer();
// ע��AutoMapper
builder.Services.AddAutoMapper(typeof(SmartSchedulingSystem.Core.Mapping.MappingProfile));
builder.Services.AddSwaggerGen();

// �������ݿ�������
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ��Program.cs�У������м������֮��
//// ���SPA֧��
//app.UseSpa(spa =>
//{
//    spa.Options.SourcePath = "ClientApp";

//    if (app.Environment.IsDevelopment())
//    {
//        // �ڿ��������У�����React����������
//        spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");

//        // ����ֱ������npm
//        // spa.UseReactDevelopmentServer(npmScript: "start");
//    }
//});
// ����CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});


// ע���ſ������Լ��

builder.Services.AddScoped<SchedulingEngine>();

// ע�����
builder.Services.AddScoped<CPScheduler>();
builder.Services.AddScoped<ConstraintManager>();
builder.Services.AddScoped<CPModelBuilder>();
builder.Services.AddScoped<SolutionConverter>();

builder.Services.AddScoped<IConstraint, TeacherConflictConstraint>();
builder.Services.AddScoped<IConstraint, ClassroomConflictConstraint>();
builder.Services.AddScoped<IConstraint, TeacherScheduleCompactnessConstraint>();


// Prerequisite
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var prereqs = db.Courses.ToDictionary(
        c => c.CourseId,
        c => c.Prerequisites.Select(p => p.PrerequisiteCourseId).ToList()
    );
    var sectionCourseMap = db.CourseSections.ToDictionary(s => s.CourseId, s => s.CourseId);
    return new PrerequisiteConstraint(prereqs, sectionCourseMap);
});

// Teacher Availability
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var avail = db.TeacherAvailabilities.ToDictionary(x => (x.TeacherId, x.TimeSlotId), x => x.IsAvailable);
    return new TeacherAvailabilityConstraint(avail);
});

// Classroom Capacity
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var cap = db.Classrooms.ToDictionary(c => c.ClassroomId, c => c.Capacity);
    var expectCount = db.CourseSections.ToDictionary(s => s.CourseSectionId, s => s.ExpectedStudentCount);
    return new ClassroomCapacityConstraint(cap, expectCount);
});

// Classroom Type Match
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var sectionTypes = db.CourseSections.ToDictionary(s => s.CourseSectionId, s => s.CourseType);
    var roomTypes = db.Classrooms.ToDictionary(c => c.ClassroomId, c => c.RoomType);
    return new ClassroomTypeMatchConstraint(sectionTypes, roomTypes);
});

// Equipment Requirement
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var sectionEquip = db.CourseSections.ToDictionary(
    s => s.CourseSectionId,
    s => s.RequiredEquipment.Select(e => e.ToString()).ToList()
);

    var roomEquip = db.Classrooms.ToDictionary(
        c => c.ClassroomId,
        c => c.AvailableEquipment.Select(e => e.ToString()).ToList()
    );
    return new EquipmentRequirementConstraint(sectionEquip, roomEquip);
});

// Location Proximity
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var teacherDept = db.Teachers.ToDictionary(t => t.TeacherId, t => t.DepartmentId);
    var buildingCampus = db.Buildings.ToDictionary(b => b.Id, b => b.CampusId);
    var travelTime = db.CampusTravelTimes.ToDictionary(ct => (ct.FromCampusId, ct.ToCampusId), ct => ct.Minutes);
    return new LocationProximityConstraint(teacherDept, buildingCampus, travelTime);
});

// Time Availability
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var unavailable = db.UnavailablePeriods
        .AsEnumerable()
        .Select(p => (p.Start, p.End, p.Reason))
        .ToList();

    var semester = db.Semesters.ToDictionary(s => s.SemesterId, s => (s.StartDate, s.EndDate));
    return new TimeAvailabilityConstraint(unavailable, semester);
});

// Teacher Preference
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var prefs = db.TeacherPreferences.ToDictionary(p => (p.TeacherId, p.TimeSlotId), p => p.PreferenceLevel);
    return new TeacherPreferenceConstraint(prefs);
});

// Teacher Workload
//���Լ����Ҫ�����ݿ��л�ȡ��ʦ�����ÿ�ܺ�ÿ�չ���Сʱ��
//���Ǽ����ʦ�������Сʱ���洢�ڽ�ʦ����
//ʵ���ϣ�������Ҫ���ݾ�������ݿ���ƽ��е���
builder.Services.AddScoped<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var weekly = db.Teachers.ToDictionary(t => t.TeacherId, t => t.MaxWeeklyHours);
    var daily = db.Teachers.ToDictionary(t => t.TeacherId, t => t.MaxDailyHours);
    return new TeacherWorkloadConstraint(weekly, daily);
});




builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IClassroomService, ClassroomService>();
builder.Services.AddScoped<ICourseSectionService, CourseSectionService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<ISchedulingConstraintService, SchedulingConstraintService>();


// TODO: ע����������ӿ�ʵ��
var app = builder.Build();

// ����HTTP����ܵ�
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseStaticFiles();
//app.UseSpaStaticFiles();

//app.UseHttpsRedirection();

app.UseCors("ReactApp");

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

// ���һ���������˵�
app.MapGet("/health", () => "Healthy");

app.Run();