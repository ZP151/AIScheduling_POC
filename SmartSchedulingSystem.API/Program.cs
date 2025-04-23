using Microsoft.OpenApi.Models;
using SmartSchedulingSystem.Core.Mapping;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器和Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "简化版课程排课API", Version = "v1" });
});

// 添加CORS策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 添加SchedulingEngine相关服务
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Engine.ConstraintManager>();

// 添加缺少的依赖项
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.CP.CPModelBuilder>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.CP.SolutionConverter>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.LS.MoveGenerator>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.LS.SimulatedAnnealingController>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.ConstraintAnalyzer>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.ParameterAdjuster>();
// 添加SolutionDiversifier服务
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.SolutionDiversifier>();

// 添加主要服务
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.CP.CPScheduler>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.LS.LocalSearchOptimizer>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.CPLSScheduler>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Utils.ProblemAnalyzer>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Engine.SolutionEvaluator>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Engine.SchedulingEngine>();

// 添加调度参数
builder.Services.AddSingleton<SmartSchedulingSystem.Scheduling.Utils.SchedulingParameters>();

// 构建应用
var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "简化版课程排课API V1"));
}

app.UseCors("ReactApp");
app.UseAuthorization();
app.MapControllers();

// 健康检查端点
app.MapGet("/health", () => "Healthy");

Console.WriteLine("简化版 Smart Scheduling System API 已启动...");

app.Run();