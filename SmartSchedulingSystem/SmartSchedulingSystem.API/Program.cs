using Microsoft.OpenApi.Models;
using SmartSchedulingSystem.Core.Mapping;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add Controller and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Simplified Course Scheduling API", Version = "v1" });
});

// Adding a CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                // Allow request source Host == current hostname or IP
                var uri = new Uri(origin);
                return uri.Host == Dns.GetHostName() || 
                       uri.Host == GetLocalIPAddress() || 
                       uri.Port == 3001; // Allow port 3001
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

// Add SchedulingEngine related services.
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Engine.ConstraintManager>();

// Add missing dependencies
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.CP.CPModelBuilder>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.CP.SolutionConverter>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.LS.MoveGenerator>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.LS.SimulatedAnnealingController>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.ConstraintAnalyzer>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.ParameterAdjuster>();
// Add Solution Diversification Services
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.SolutionDiversifier>();

// Add major services
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.CP.CPScheduler>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.LS.LocalSearchOptimizer>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Algorithms.Hybrid.CPLSScheduler>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Utils.ProblemAnalyzer>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Engine.SolutionEvaluator>();
builder.Services.AddScoped<SmartSchedulingSystem.Scheduling.Engine.SchedulingEngine>();

// Add scheduling parameters
builder.Services.AddSingleton<SmartSchedulingSystem.Scheduling.Utils.SchedulingParameters>();


// Build the application
var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Simplified Course Scheduling API V1"));
}

app.UseCors("ReactApp");

app.UseStaticFiles(); // ✅ Allow use of wwwroot static files

app.UseAuthorization();
app.MapControllers();

// ✅ Add fallback mapping to React's index.html
app.MapFallbackToFile("index.html");

// Health check endpoint
app.MapGet("/health", () => "Healthy");

Console.WriteLine("Simplified Smart Scheduling System API has started...");

app.Run();