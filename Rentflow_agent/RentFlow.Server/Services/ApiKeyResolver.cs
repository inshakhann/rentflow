using System;
using System.Collections.Generic;
using System.Linq;
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

        public static IReadOnlyList<string> ResolveMany(IConfiguration configuration, string configPath, params string[] fallbackEnvironmentVariables)
        {
            var keys = new List<string>();

            // Support both section arrays and single comma-separated values in configuration.
            var sectionValues = configuration.GetSection(configPath).GetChildren()
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v));
            foreach (var value in sectionValues)
            {
                keys.AddRange(SplitKeys(value!));
            }

            var configured = configuration[configPath];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                keys.AddRange(SplitKeys(configured));
            }

            foreach (var envVar in fallbackEnvironmentVariables)
            {
                var envValue = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    keys.AddRange(SplitKeys(envValue));
                }
            }

            return keys
                .Select(Normalize)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
        }

        private static IEnumerable<string> SplitKeys(string value)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(v => !string.IsNullOrWhiteSpace(v));
        }
    }
}
