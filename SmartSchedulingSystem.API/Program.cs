using Microsoft.OpenApi.Models;
using SmartSchedulingSystem.Core.Mapping;
using System.Net;

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
        policy
            .SetIsOriginAllowed(origin =>
            {
                // 允许请求来源的 Host == 当前主机名或 IP
                var uri = new Uri(origin);
                return uri.Host == Dns.GetHostName() || 
                       uri.Host == GetLocalIPAddress() || 
                       uri.Port == 3001; // 允许3001端口
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

string GetLocalIPAddress()
{
    return Dns.GetHostEntry(Dns.GetHostName())
        .AddressList
        .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        ?.ToString() ?? "127.0.0.1";
}

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

app.UseStaticFiles(); // ✅ 允许使用 wwwroot 静态文件

app.UseAuthorization();
app.MapControllers();

// ✅ 添加 fallback 映射到 React 的 index.html
app.MapFallbackToFile("index.html");

// 健康检查端点
app.MapGet("/health", () => "Healthy");

Console.WriteLine("简化版 Smart Scheduling System API 已启动...");

app.Run();