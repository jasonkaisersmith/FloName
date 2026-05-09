// ServiceConfiguration.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;

namespace FloName.Api
{
    public static class ServiceConfiguration
    {
        public static void AddFloNameServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<FilenameGenerator>(sp => {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<FilenameGenerator>>();
                var path = config["FloName:DictsPath"]
                    ?? Path.Combine(AppContext.BaseDirectory, "dicts");

                if (!Directory.Exists(path))
                    throw new InvalidOperationException(
                        $"Dicts directory not found at '{path}'.");

                return new FilenameGenerator(path, logger);
            });
        }

        public static void AddFloNameCors(this WebApplicationBuilder builder)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Open", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        public static void AddFloNameHealthChecks(this WebApplicationBuilder builder)
        {
            builder.Services.AddHealthChecks()
                .AddCheck<FloNameHealthCheck>("floname");
        }

        public static void AddFloNameTimeouts(this WebApplicationBuilder builder)
        {
            builder.Services.AddRequestTimeouts(options =>
            {
                options.DefaultPolicy = new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(5),
                    TimeoutStatusCode = 408,
                    WriteTimeoutResponse = async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(
                            ApiResponse<string>.Fail("Request timed out."));
                    }
                };

                options.AddPolicy("batch", new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(30),
                    TimeoutStatusCode = 408,
                    WriteTimeoutResponse = async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(
                            ApiResponse<string>.Fail("Batch request timed out."));
                    }
                });
            });
        }

        public static void AddFloNameVersioning(this WebApplicationBuilder builder)
        {
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("X-Api-Version")
                );
            });
        }
    }
}