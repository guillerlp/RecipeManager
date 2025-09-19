using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace RecipeManager.Api.Middlewares
{
    public sealed class ErrorHandlerMiddleware
    {
        private readonly ILogger<ErrorHandlerMiddleware> _logger;
        private readonly RequestDelegate _next;

        public ErrorHandlerMiddleware(ILogger<ErrorHandlerMiddleware> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}", 
                    context.Request.Path, context.Request.Method);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Bad request"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                _ => (StatusCodes.Status500InternalServerError, "Internal server error")
            };
    
            context.Response.StatusCode = statusCode;
            
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Type = exception.GetType().Name,
                    Title = title,
                    Detail = exception.Message,
                    Status = statusCode
                });
        }
    }
}