using ServiceAbstraction_Layer;
using Services_Layer;
using ThreatDetectionSystem.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ================= Controllers =================
builder.Services.AddControllers();

// ================= HttpClient =================
builder.Services.AddHttpClient<IElkService, ElkService>();

// ================= SignalR =================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// ================= DI =================
builder.Services.AddScoped<IAlertHubNotifier, AlertHubNotifier>();
builder.Services.AddHostedService<AlertBroadcastService>();

// ================= CORS (IMPORTANT FIX) =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .WithOrigins(
                 "http://localhost:5173"   // ✅ Vite الصحيح
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ================= Swagger =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ================= Middleware ORDER (CRITICAL) =================

// 1. Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2. Routing FIRST
app.UseRouting();

// 3. CORS MUST be AFTER routing and BEFORE auth/endpoints
app.UseCors("ReactApp");

// 4. Authorization (if needed)
app.UseAuthorization();

// 5. Map endpoints
app.MapControllers();

// IMPORTANT: SignalR Hub
app.MapHub<AlertHub>("/alertHub")
   .RequireCors("ReactApp");   // 🔥 مهم جدًا لحل مشكلة negotiate

app.Run();