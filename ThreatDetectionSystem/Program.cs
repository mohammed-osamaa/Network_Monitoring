using Microsoft.EntityFrameworkCore;
using ServiceAbstraction_Layer;
using Services_Layer;
using ThreatDetectionSystem.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<IElkService, ElkService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IAlertHubNotifier, AlertHubNotifier>();
builder.Services.AddHostedService<AlertBroadcastService>();

builder.Services.AddCors(options =>
    options.AddPolicy("ReactApp", policy =>
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactApp");        // ← أضف ده

app.MapControllers();
app.MapHub<AlertHub>("/alertHub");  // ← أضف ده

app.Run();