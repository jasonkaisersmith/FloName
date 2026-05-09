using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FloName.Api
{
    public static class EndpointsConfiguration
    {
        public static void MapGenerateEndpoints(this WebApplication app, ApiVersionSet apiV1)
        {
            app.MapGet("/generate/{lang}", (string lang, string format, string? extension,
                FilenameGenerator generator, ILogger<Program> logger) =>
            {
                var invalid = RequestValidator.ValidateGenerateRequest(lang, format, generator);
                if (invalid != null)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning("Invalid generate request. Lang: {Lang}, Format: {Format}", lang, format);
                    return invalid;
                }

                try
                {
                    var result = generator.For(lang)
                        .WithFormat(format)
                        .WithExtension(extension)
                        .Generate();

                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Generated 1 result for lang {Lang}", lang);
                    return Results.Ok(ApiResponse<string>.Ok(result));
                }
                catch (ArgumentException ex)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning(ex, "Bad argument in generate request. Lang: {Lang}, Format: {Format}", lang, format);
                    return Results.BadRequest(ApiResponse<string>.Fail(ex.Message));
                }
                catch (FormatException ex)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning(ex, "Format error in generate request. Lang: {Lang}, Format: {Format}", lang, format);
                    return Results.BadRequest(ApiResponse<string>.Fail(ex.Message));
                }
                catch (InvalidOperationException ex)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning(ex, "Operation error in generate request. Lang: {Lang}, Format: {Format}", lang, format);
                    return Results.BadRequest(ApiResponse<string>.Fail(ex.Message));
                }
            })
            .WithRequestTimeout(TimeSpan.FromSeconds(5))
            .WithApiVersionSet(apiV1)
            .MapToApiVersion(1, 0);

            app.MapGet("/batch/{lang}", (string lang, string format, int count, string? extension,
                FilenameGenerator generator, ILogger<Program> logger) =>
            {
                var invalid = RequestValidator.ValidateBatchRequest(lang, format, count, generator);
                if (invalid != null)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning("Invalid batch request. Lang: {Lang}, Format: {Format}, Count: {Count}", lang, format, count);
                    return invalid;
                }

                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var result = generator.For(lang)
                        .WithFormat(format)
                        .WithExtension(extension)
                        .GenerateBatch(count);
                    sw.Stop();
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Generated batch of {Count} names for lang {Lang} in {Ms}ms",
                            count, lang, sw.ElapsedMilliseconds);
                    return Results.Ok(ApiResponse<IReadOnlyList<string>>.Ok(result));
                }
                catch (ArgumentException ex)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning(ex, "Bad argument in batch request. Lang: {Lang}, Format: {Format}, Count: {Count}",
                            lang, format, count);
                    return Results.BadRequest(ApiResponse<IReadOnlyList<string>>.Fail(ex.Message));
                }
                catch (FormatException ex)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning(ex, "Format error in batch request. Lang: {Lang}, Format: {Format}, Count: {Count}",
                            lang, format, count);
                    return Results.BadRequest(ApiResponse<IReadOnlyList<string>>.Fail(ex.Message));
                }
                catch (InvalidOperationException ex)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                        logger.LogWarning(ex, "Operation error in batch request. Lang: {Lang}, Format: {Format}, Count: {Count}",
                            lang, format, count);
                    return Results.BadRequest(ApiResponse<IReadOnlyList<string>>.Fail(ex.Message));
                }
            })
            .WithRequestTimeout("batch")
            .WithApiVersionSet(apiV1)
            .MapToApiVersion(1, 0);
        }
    

        public static void MapUtilityEndpoints(this WebApplication app ) //, ApiVersionSet apiV1)
        {
            ///Language Suppport
            app.MapGet("/languages", (FilenameGenerator generator, ILogger<Program> logger) =>
            {
                try
                {
                    var languages = generator.SupportedLanguages();
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Returning {Count} supported languages", languages.Count);
                    return Results.Ok(ApiResponse<IReadOnlyList<string>>.Ok(languages));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving supported languages");
                    return Results.StatusCode(500);
                }
            })
            //.WithApiVersionSet(apiV1)
            //.MapToApiVersion(1, 0)
            .WithRequestTimeout(TimeSpan.FromSeconds(5));

            ///Help
            app.MapGet("/help", () =>
                Results.Ok(ApiResponse<HelpResponse>.Ok(FilenameGenerator.GetHelp())))
//                .WithApiVersionSet(apiV1)
  //              .MapToApiVersion(1, 0)
                .WithRequestTimeout(TimeSpan.FromSeconds(5));

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description
                        })
                    });
                }
            })
            .WithRequestTimeout(TimeSpan.FromSeconds(5));
        }
    }
}
