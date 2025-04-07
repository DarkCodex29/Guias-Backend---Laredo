using System.Diagnostics;
using System.Security.Claims;

namespace GuiasBackend.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Log detallado al inicio
            LogRequestDetails(context);
            
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                var statusCode = context.Response?.StatusCode;
                
                LogResponseDetails(context, statusCode, elapsedMilliseconds);
            }
        }
        
        private void LogRequestDetails(HttpContext context)
        {
            // Log básico de la solicitud
            _logger.LogInformation(
                "Recibida solicitud {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            // Log detallado del token de autorización
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString().Replace("Bearer ", "");
                _logger.LogInformation(
                    "Token recibido: {TokenLength} caracteres, Comienza con: {TokenStart}",
                    token.Length,
                    token.Length > 10 ? token.Substring(0, 10) + "..." : token);
            }
            else
            {
                _logger.LogInformation("No se encontró token de autorización en la solicitud");
            }
        }
        
        private void LogResponseDetails(HttpContext context, int? statusCode, long elapsedMilliseconds)
        {
            LogLevel level;
            if (statusCode > 499)
            {
                level = LogLevel.Error;
            }
            else if (statusCode == 401 || statusCode == 403)
            {
                level = LogLevel.Warning;
            }
            else
            {
                level = LogLevel.Information;
            }

            if (statusCode == 401 || statusCode == 403)
            {
                if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    string tokenPresent = !string.IsNullOrEmpty(authHeader) ? "presente" : "vacío";
                    _logger.Log(level, 
                        "HTTP {Method} {Path} respondió {StatusCode} en {Elapsed:0.0000} ms. Token {TokenPresent}",
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        elapsedMilliseconds,
                        tokenPresent);
                }
                else
                {
                    _logger.Log(level, 
                        "HTTP {Method} {Path} respondió {StatusCode} en {Elapsed:0.0000} ms. Sin token",
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        elapsedMilliseconds);
                }
            }
            else
            {
                _logger.Log(level,
                    "HTTP {Method} {Path} respondió {StatusCode} en {Elapsed:0.0000} ms",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    elapsedMilliseconds);
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}