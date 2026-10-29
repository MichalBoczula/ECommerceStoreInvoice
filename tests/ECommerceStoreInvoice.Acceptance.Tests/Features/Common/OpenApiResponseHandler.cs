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

        var expected = (int)response.StatusCode >= 400 ? "application/problem+json" :
            operation.GetProperty("operationId").GetString() == "DownloadInvoicePdf" ? "application/pdf" : "application/json";
        var actual = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) || !declared.TryGetProperty("content", out var content) ||
            !content.TryGetProperty(expected, out var representation) ||
            !representation.TryGetProperty("schema", out var schema))
            throw new InvalidOperationException($"Response media or schema differs from OpenAPI: {request.Method} {path} {status} ({actual})");

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (expected == "application/pdf")
        {
            ValidatePdf(schema, response, bytes, path);
            return response;
        }
        using var body = JsonDocument.Parse(bytes);
        ValidateJson(schema, body.RootElement, document.RootElement.GetProperty("components").GetProperty("schemas"), path);
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

    internal static void ValidateJson(JsonElement schema, JsonElement value, JsonElement components, string path)
    {
        var errors = JsonContractValidator.Check(schema, value, components);
        if (errors.Count > 0)
            throw new InvalidOperationException($"Response contract mismatch at {path}: {string.Join("; ", errors)}");
    }

    private static void ValidatePdf(JsonElement schema, HttpResponseMessage response, byte[] bytes, string path)
    {
        if (schema.GetProperty("type").GetString() != "string" || schema.GetProperty("format").GetString() != "binary")
            throw new InvalidOperationException("PDF response schema must declare binary bytes.");
        if (!bytes.AsSpan().StartsWith("%PDF-"u8))
            throw new InvalidOperationException("Invoice download is not a PDF.");
        var invoiceId = Guid.Parse(path.Split('/', StringSplitOptions.RemoveEmptyEntries)[1]);
        var filename = $"invoice-{invoiceId:D}.pdf";
        var disposition = response.Content.Headers.ContentDisposition;
        if (disposition?.DispositionType != "attachment" || disposition.FileNameStar != filename || disposition.FileName?.Trim('"') != filename)
            throw new InvalidOperationException("Invoice PDF attachment filename differs from the route invoice.");
        var cache = response.Headers.CacheControl?.ToString().Split(',').Select(value => value.Trim()).Order().ToArray();
        if (cache is null || !cache.SequenceEqual(new[] { "no-store", "private" }))
            throw new InvalidOperationException("Invoice PDF must use private, no-store caching.");
    }
}
