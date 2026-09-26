using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

internal sealed class OpenApiResponseHandler(JsonDocument document) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var method = request.Method.Method.ToLowerInvariant();
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            return response;

        // The old create-order path remains a compatibility alias outside the canonical OpenAPI document.
        var lookupPath = method == "post" && path.StartsWith("/orders/", StringComparison.OrdinalIgnoreCase) &&
                         path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length == 2
            ? "/orders/client/" + path.Split('/').Last()
            : path;
        var route = document.RootElement.GetProperty("paths").EnumerateObject()
            .FirstOrDefault(candidate => Matches(candidate.Name, lookupPath) &&
                                         candidate.Value.TryGetProperty(method, out _));
        if (route.Value.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException($"HTTP operation absent from OpenAPI: {request.Method} {path}");

        var operation = route.Value.GetProperty(method);
        var status = ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!operation.GetProperty("responses").TryGetProperty(status, out var declared))
            throw new InvalidOperationException($"Undeclared runtime status: {operation.GetProperty("operationId").GetString()} {status}");

        var expected = (int)response.StatusCode >= 400 ? "application/problem+json" : "application/json";
        var actual = response.Content.Headers.ContentType?.MediaType;
        if (actual != expected || !declared.TryGetProperty("content", out var content) ||
            !content.TryGetProperty(expected, out var representation) ||
            !representation.TryGetProperty("schema", out var schema))
            throw new InvalidOperationException($"Response media or schema differs from OpenAPI: {request.Method} {path} {status} ({actual})");

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        using var body = JsonDocument.Parse(bytes);
        Validate(schema, body.RootElement, document.RootElement.GetProperty("components").GetProperty("schemas"), path, 0);
        return response;
    }

    private static bool Matches(string template, string path)
    {
        var expected = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var actual = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return expected.Length == actual.Length && expected.Zip(actual).All(parts =>
            parts.First.StartsWith('{') && parts.First.EndsWith('}') ||
            string.Equals(parts.First, parts.Second, StringComparison.OrdinalIgnoreCase));
    }

    private static void Validate(JsonElement schema, JsonElement value, JsonElement components, string path, int depth)
    {
        if (depth > 16)
            return;
        if (schema.TryGetProperty("$ref", out var reference))
        {
            Validate(components.GetProperty(reference.GetString()!.Split('/').Last()), value, components, path, depth + 1);
            return;
        }
        if (schema.TryGetProperty("allOf", out var allOf))
            foreach (var part in allOf.EnumerateArray())
                Validate(part, value, components, path, depth + 1);
        if (!schema.TryGetProperty("type", out var type))
            return;
        if (type.GetString() == "array")
        {
            if (value.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException($"Expected array at {path}");
            if (schema.TryGetProperty("items", out var items))
                foreach (var element in value.EnumerateArray())
                    Validate(items, element, components, path, depth + 1);
        }
        if (type.GetString() == "object")
        {
            if (value.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"Expected object at {path}");
            if (schema.TryGetProperty("required", out var required))
                foreach (var field in required.EnumerateArray())
                    if (!value.TryGetProperty(field.GetString()!, out _))
                        throw new InvalidOperationException($"Required response field missing at {path}: {field.GetString()}");
            if (schema.TryGetProperty("properties", out var properties))
                foreach (var property in properties.EnumerateObject())
                    if (value.TryGetProperty(property.Name, out var child) && child.ValueKind != JsonValueKind.Null)
                        Validate(property.Value, child, components, path, depth + 1);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            document.Dispose();
        base.Dispose(disposing);
    }
}
