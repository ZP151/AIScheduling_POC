using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchedulSmartSchedulingSystemingSystem.Scheduling.Constraints.Soft;
using SmartSchedulingSystem.API.Middleware;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Core.Services;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Scheduling;
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

// ���ӷ�������
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
//// ����SPA֧��
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
// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // React默认端口
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// 注册服务，避免生命周期冲突问题
// ConstraintManager是singleton，所以依赖的IConstraint实现也必须是singleton

// 修改服务注册
builder.Services.AddTransient<SchedulingEngine>();
builder.Services.AddTransient<CPScheduler>();
builder.Services.AddSingleton<ConstraintManager>();
builder.Services.AddTransient<CPModelBuilder>();
builder.Services.AddSingleton<SolutionConverter>();
// 注册ISolutionEvaluator接口，解决ConflictResolver依赖问题
builder.Services.AddSingleton<ISolutionEvaluator, SolutionEvaluator>();

// 注册冲突解析器和处理器为Singleton
builder.Services.AddSingleton<ConflictResolver>();
builder.Services.AddSingleton<IConflictHandler, TeacherConflictHandler>();
builder.Services.AddSingleton<IConflictHandler, ClassroomConflictHandler>();

// 修改依赖注入生命周期：IConstraint必须是Singleton，否则会出现生命周期依赖冲突
builder.Services.AddSingleton<IConstraint, TeacherConflictConstraint>();
builder.Services.AddSingleton<IConstraint, ClassroomConflictConstraint>();
builder.Services.AddSingleton<IConstraint, TeacherScheduleCompactnessConstraint>();


// Prerequisite
builder.Services.AddSingleton<IConstraint>(sp =>
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
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var teacherAva = db.TeacherAvailabilities.ToDictionary(
        ta => (ta.TeacherId, ta.TimeSlotId),
        ta => ta.IsAvailable
    );
    return new TeacherAvailabilityConstraint(teacherAva);
});

// Classroom Capacity
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var courseEnroll = db.CourseSections.ToDictionary(c => c.CourseSectionId, c => c.ExpectedStudentCount);
    var roomCapacity = db.Classrooms.ToDictionary(r => r.ClassroomId, r => r.Capacity);
    return new ClassroomCapacityConstraint(courseEnroll, roomCapacity);
});

// Classroom Type Match
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var sectionTypes = db.CourseSections.ToDictionary(s => s.CourseSectionId, s => s.CourseType);
    var roomTypes = db.Classrooms.ToDictionary(c => c.ClassroomId, c => c.RoomType);
    return new ClassroomTypeMatchConstraint(sectionTypes, roomTypes);
});

// Equipment Requirement
builder.Services.AddSingleton<IConstraint>(sp =>
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
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var teacherDept = db.Teachers.ToDictionary(t => t.TeacherId, t => t.DepartmentId);
    var buildingCampus = db.Buildings.ToDictionary(b => b.Id, b => b.CampusId);
    var travelTime = db.CampusTravelTimes.ToDictionary(ct => (ct.FromCampusId, ct.ToCampusId), ct => ct.Minutes);
    return new LocationProximityConstraint(teacherDept, buildingCampus, travelTime);
});

// Time Availability
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var unavailable = db.UnavailablePeriods
        .AsEnumerable()
        .Select(p => (p.Start, p.End, p.Reason))
        .ToList();

    var semester = db.Semesters.ToDictionary(s => s.SemesterId, s => (s.StartDate, s.EndDate));
    return new TimeAvailabilityConstraint(unavailable, semester);
});

// Teacher Preference - 修改为Singleton
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var prefs = db.TeacherPreferences.ToDictionary(p => (p.TeacherId, p.TimeSlotId), p => p.PreferenceLevel);
    return new TeacherPreferenceConstraint(prefs);
});

// Teacher Workload - 修改为Singleton
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var weekly = db.Teachers.ToDictionary(t => t.TeacherId, t => t.MaxWeeklyHours);
    var daily = db.Teachers.ToDictionary(t => t.TeacherId, t => t.MaxDailyHours);
    return new TeacherWorkloadConstraint(weekly, daily);
});

// Classroom Availability
builder.Services.AddSingleton<IConstraint>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var classroomAva = db.ClassroomAvailabilities.ToDictionary(
        ca => (ca.ClassroomId, ca.TimeSlotId),
        ca => ca.IsAvailable
    );
    return new ClassroomAvailabilityConstraint(classroomAva);
});

builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IClassroomService, ClassroomService>();
builder.Services.AddScoped<ICourseSectionService, CourseSectionService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<ISchedulingConstraintService, SchedulingConstraintService>();

// 注册排课系统服务
builder.Services.AddSchedulingServices();

// TODO: עӿʵ
var app = builder.Build();

// HTTPܵ
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

// һ˵
app.MapGet("/health", () => "Healthy");

app.Run();