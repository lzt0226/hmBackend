using Microsoft.AspNetCore.Mvc;
using PatientMonitor.Api.Services;

namespace PatientMonitor.Api.Middleware;

/// <summary>
/// 全局异常处理中间件。捕获未处理的异常，返回与原 Java 项目兼容的 JSON 错误格式。
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { code = 404, message = ex.Message });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { code = 500, message = ex.Message });
        }
    }
}
