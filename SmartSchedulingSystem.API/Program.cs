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