using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FloName.Api
{
    internal class FloNameHealthCheck : IHealthCheck
    {
        private readonly FilenameGenerator _generator;

        public FloNameHealthCheck(FilenameGenerator generator)
        {
            _generator = generator;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var languages = _generator.SupportedLanguages();

            if (languages?.Count == 0)
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("No dictionaries loaded."));

            return Task.FromResult(
                HealthCheckResult.Healthy($"Loaded {languages!.Count} language(s): {string.Join(", ", languages!)}"));
        }
    }
}
