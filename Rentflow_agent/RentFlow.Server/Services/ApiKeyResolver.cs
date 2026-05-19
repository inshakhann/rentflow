using System;
using Microsoft.Extensions.Configuration;

namespace RentFlow.Server.Services
{
    public static class ApiKeyResolver
    {
        public static string? Resolve(IConfiguration configuration, string configPath, params string[] fallbackEnvironmentVariables)
        {
            var configured = Normalize(configuration[configPath]);
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            foreach (var envVar in fallbackEnvironmentVariables)
            {
                var envValue = Normalize(Environment.GetEnvironmentVariable(envVar));
                if (!string.IsNullOrWhiteSpace(envValue))
                    return envValue;
            }

            return null;
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
        }
    }
}
