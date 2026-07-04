using Microsoft.EntityFrameworkCore;
using PatientMonitor.Api.Data;
using PatientMonitor.Api.Middleware;
using PatientMonitor.Api.Services;
using PatientMonitor.Api.WebSockets;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// 配置监听地址（从 appsettings.json 读取，默认 8080 端口）
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://0.0.0.0:8080");

// ==================== 服务注册 ====================

// SQLite 数据库
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=patient_monitor.db"));

// 业务服务
builder.Services.AddScoped<IBehaviorLogService, BehaviorLogService>();
builder.Services.AddScoped<IPatientService, PatientService>();

// WebSocket Handler 单例
builder.Services.AddSingleton<MonitorWebSocketHandler>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "患者监控系统API",
        Description = "患者监控系统接口文档",
        Version = "1.0.0"
    });
});

var app = builder.Build();

// ==================== 数据库初始化 ====================
AppDbContext.Initialize(app.Services);

// ==================== 中间件管道 ====================

// 全局异常处理
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger（始终启用）
app.UseSwagger();
app.UseSwaggerUI();

// WebSocket 支持
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(45)
});

// 映射 WebSocket 端点 /ws/monitor
app.Map("/ws/monitor", async (HttpContext context, MonitorWebSocketHandler handler) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleConnectionAsync(webSocket, context.RequestAborted);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

var wsHandler = app.Services.GetRequiredService<MonitorWebSocketHandler>();
wsHandler.Start();

// 路由 + 控制器
app.UseRouting();
app.MapControllers();


app.Lifetime.ApplicationStopping.Register(() =>
{
    wsHandler.Stop();
});

app.Run();
