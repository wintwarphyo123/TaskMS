using System.Net;
using System.Text.Json;
using TaskManagement.API.DTOs;

namespace TaskManagement.API.Middleware
{
    public class GlobalExceptionMiddleware(RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context); // Request ကို နောက် Middleware/Controller ဆီ ဆက်ပို့ပေးမည်
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ErrorResponseDto
            {
                StatusCode = context.Response.StatusCode,
                Message = "An internal server error occurred. Please try again later.",
                // Development Environment ဆိုပါက error အတိအကျပြပြီး Production ဆိုပါက ဖုံးကွယ်ထားမည်
                Details = env.IsDevelopment() ? exception.StackTrace : null
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            return context.Response.WriteAsync(json);
        }
    }
}
