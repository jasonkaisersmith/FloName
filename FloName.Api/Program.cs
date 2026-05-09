using Asp.Versioning;
using Serilog;

namespace FloName.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
            builder.Host.UseSerilog();

            builder.AddFloNameServices();
            builder.AddFloNameCors();
            builder.AddFloNameHealthChecks();
            builder.AddFloNameTimeouts();
            builder.AddFloNameVersioning();

            var app = builder.Build();

            app.UseFloNameExceptionHandler();
            app.UseCors("Open");
            app.UseHttpsRedirection();
            app.UseRequestTimeouts();
            app.UseFloNameRequestLogging();

            // Force eager initialisation
            app.Services.GetRequiredService<FilenameGenerator>();

            var apiV1 = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            app.MapGenerateEndpoints(apiV1);
            app.MapUtilityEndpoints(); // (apiV1);

            app.Run();
        }
    }
}