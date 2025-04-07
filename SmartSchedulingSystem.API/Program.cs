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

// 添加服务到容器
builder.Services.AddControllers();

// 配置Swagger
builder.Services.AddEndpointsApiExplorer();
// 注册AutoMapper
builder.Services.AddAutoMapper(typeof(SmartSchedulingSystem.Core.Mapping.MappingProfile));
builder.Services.AddSwaggerGen();

// 配置数据库上下文
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 在Program.cs中，其他中间件配置之后
//// 添加SPA支持
//app.UseSpa(spa =>
//{
//    spa.Options.SourcePath = "ClientApp";

//    if (app.Environment.IsDevelopment())
//    {
//        // 在开发环境中，启动React开发服务器
//        spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");

//        // 或者直接启动npm
//        // spa.UseReactDevelopmentServer(npmScript: "start");
//    }
//});
// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});


// 注册排课引擎和约束

builder.Services.AddScoped<SchedulingEngine>();

// 注册服务
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
//这个约束需要从数据库中获取教师的最大每周和每日工作小时数
//我们假设教师的最大工作小时数存储在教师表中
//实际上，可能需要根据具体的数据库设计进行调整
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


// TODO: 注册其他服务接口实现
var app = builder.Build();

// 配置HTTP请求管道
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

// 添加一个健康检查端点
app.MapGet("/health", () => "Healthy");

app.Run();