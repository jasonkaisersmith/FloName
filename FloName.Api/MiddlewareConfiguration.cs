// MiddlewareConfiguration.cs
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace FloName.Api
{
    public static class MiddlewareConfiguration
    {
        public static void UseFloNameExceptionHandler(this WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var error = context.Features.Get<IExceptionHandlerFeature>();
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                    if (error?.Error != null)
                        logger.LogError(error.Error, "Unhandled exception on {Method} {Path}",
                            context.Request.Method, context.Request.Path);

                    var message = app.Environment.IsDevelopment()
                        ? error?.Error.Message ?? "An unexpected error occurred."
                        : "An unexpected error occurred.";

                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(ApiResponse<string>.Fail(message));
                });
            });
        }

        public static void UseFloNameRequestLogging(this WebApplication app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                    diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
                };

                options.GetLevel = (httpContext, elapsed, ex) =>
                    httpContext.Request.Path.StartsWithSegments("/health")
                        ? Serilog.Events.LogEventLevel.Debug
                        : ex != null
                            ? Serilog.Events.LogEventLevel.Error
                            : Serilog.Events.LogEventLevel.Information;
            });
        }
    }
}