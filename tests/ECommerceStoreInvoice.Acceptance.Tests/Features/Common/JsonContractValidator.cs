using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

/// <summary>Validates the OpenAPI 3.0 schema vocabulary emitted by this service.</summary>
internal static class JsonContractValidator
{
    internal static IReadOnlyList<string> Check(JsonElement schema, JsonElement value, JsonElement components, bool request = false, int initialDepth = 0)
    {
        var errors = new List<string>();
        Visit(schema, value, "$", initialDepth);
        return errors;

        void Visit(JsonElement rule, JsonElement actual, string path, int depth)
        {
            if (depth > 64)
                throw new InvalidOperationException($"Contract nesting limit exceeded at {path}");
            if (rule.TryGetProperty("anyOf", out _) || rule.TryGetProperty("not", out _))
                throw new InvalidOperationException($"Unsupported schema composition at {path}");
            if (actual.ValueKind == JsonValueKind.Null && rule.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean())
                return;
            if (rule.TryGetProperty("oneOf", out var alternatives))
            {
                var matchedAlternatives = alternatives.EnumerateArray().Count(alternative => Check(alternative, actual, components, request, depth + 1).Count == 0);
                if (matchedAlternatives != 1) errors.Add($"{path}: expected exactly one schema alternative, matched {matchedAlternatives}");
                return;
            }
            if (rule.TryGetProperty("$ref", out var reference))
            {
                const string prefix = "#/components/schemas/";
                var target = reference.GetString()!;
                if (!target.StartsWith(prefix, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Unsupported schema reference: {target}");
                Visit(components.GetProperty(target[prefix.Length..]), actual, path, depth + 1);
                return;
            }
            if (rule.TryGetProperty("allOf", out var allOf))
                foreach (var part in allOf.EnumerateArray())
                    Visit(part, actual, path, depth + 1);
            if (!rule.TryGetProperty("type", out var type))
                return; // An empty additionalProperties schema deliberately permits any JSON value.

            var expected = type.GetString();
            var matches = expected switch
            {
                "object" => actual.ValueKind == JsonValueKind.Object,
                "array" => actual.ValueKind == JsonValueKind.Array,
                "string" => actual.ValueKind == JsonValueKind.String,
                "boolean" => actual.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "integer" => actual.ValueKind == JsonValueKind.Number && actual.TryGetInt64(out _),
                "number" => actual.ValueKind == JsonValueKind.Number && actual.TryGetDouble(out var number) && double.IsFinite(number),
                _ => throw new InvalidOperationException($"Unsupported schema type: {expected}")
            };
            if (!matches)
            {
                errors.Add($"{path}: expected {expected}, observed {actual.ValueKind}");
                return;
            }
            if (rule.TryGetProperty("enum", out var choices) && !choices.EnumerateArray().Any(choice => JsonElement.DeepEquals(choice, actual)))
                errors.Add($"{path}: value is outside the declared enum");
            if (rule.TryGetProperty("format", out var format))
            {
                var valid = format.GetString() switch
                {
                    "uuid" => actual.ValueKind == JsonValueKind.String && Guid.TryParseExact(actual.GetString(), "D", out _),
                    "date-time" => actual.ValueKind == JsonValueKind.String && IsDateTime(actual.GetString()!),
                    "int32" => actual.ValueKind == JsonValueKind.Number && actual.TryGetInt32(out _),
                    "int64" => actual.ValueKind == JsonValueKind.Number && actual.TryGetInt64(out _),
                    "uri" => actual.ValueKind == JsonValueKind.String && Uri.TryCreate(actual.GetString(), UriKind.Absolute, out _),
                    "double" or "float" => actual.ValueKind == JsonValueKind.Number && actual.TryGetDouble(out var formattedNumber) && double.IsFinite(formattedNumber),
                    _ => throw new InvalidOperationException($"Unsupported schema format: {format.GetString()}")
                };
                if (!valid)
                    errors.Add($"{path}: invalid {format.GetString()} format");
            }
            if (expected == "array")
            {
                if (!rule.TryGetProperty("items", out var items))
                    throw new InvalidOperationException($"Array schema has no items at {path}");
                var index = 0;
                foreach (var item in actual.EnumerateArray())
                    Visit(items, item, $"{path}[{index++}]", depth + 1);
            }
            if (expected != "object")
                return;

            var members = actual.EnumerateObject().ToArray();
            if (!request && members.GroupBy(member => member.Name, StringComparer.Ordinal).Any(group => group.Count() != 1))
                errors.Add($"{path}: duplicate response member");
            var comparison = request ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (rule.TryGetProperty("required", out var required))
                foreach (var field in required.EnumerateArray())
                    if (!members.Any(member => string.Equals(member.Name, field.GetString(), comparison)))
                        errors.Add($"{path}.{field.GetString()}: required member is missing");
            var properties = rule.TryGetProperty("properties", out var declared)
                ? declared.EnumerateObject().ToArray() : [];
            foreach (var member in members)
            {
                var property = properties.FirstOrDefault(property => string.Equals(property.Name, member.Name, comparison));
                if (property.Value.ValueKind != JsonValueKind.Undefined)
                    Visit(property.Value, member.Value, $"{path}.{member.Name}", depth + 1);
                else if (rule.TryGetProperty("additionalProperties", out var extra))
                {
                    if (extra.ValueKind == JsonValueKind.False)
                        errors.Add($"{path}.{member.Name}: additional member is not allowed");
                    else if (extra.ValueKind == JsonValueKind.Object)
                        Visit(extra, member.Value, $"{path}.{member.Name}", depth + 1);
                }
            }
        }
    }

    private static bool IsDateTime(string value)
        => Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant) &&
           DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
