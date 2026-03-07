using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ECommerceStoreInvoice.API.Configuration.Extensions
{
    public static class JsonDeserializationExceptionHandlerExtension
    {
        public static async Task HandleJsonDeserializationException(this HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var jsonException = exception as JsonException ?? exception.InnerException as JsonException;
            var (typeName, missingProperties) = ExtractMissingInformation(jsonException?.Message);
            var detail = BuildDetail(typeName, missingProperties, jsonException?.Message ?? exception.Message);

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid JSON payload.",
                Detail = detail,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            }, cancellationToken);
        }

        private static (string TypeName, IReadOnlyCollection<string> MissingProperties) ExtractMissingInformation(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return (string.Empty, Array.Empty<string>());
            }

            var match = Regex.Match(message, @"type '([^']+)' was missing required properties including:(.+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return (string.Empty, Array.Empty<string>());
            }

            var typeName = match.Groups[1].Value;
            var missingRaw = match.Groups[2].Value;
            var missingProperties = missingRaw
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Replace("'", string.Empty, StringComparison.Ordinal).Trim(' ', '.', ';'))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            return (typeName, missingProperties);
        }

        private static string BuildDetail(string typeName, IReadOnlyCollection<string> missingProperties, string fallbackMessage)
        {
            if (missingProperties.Count == 0)
            {
                return fallbackMessage;
            }

            var simpleTypeName = string.IsNullOrWhiteSpace(typeName)
                ? "payload"
                : typeName.Split('.').Last();

            return $"JSON payload for {simpleTypeName} is missing required properties: {string.Join(", ", missingProperties)}.";
        }
    }
}
